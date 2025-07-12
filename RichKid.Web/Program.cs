using RichKid.Web.Services;
using RichKid.Shared.Services; // Keep using shared interfaces for consistency
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Configure comprehensive logging for the web application
builder.Logging.ClearProviders(); // Start fresh with no default providers
builder.Logging.AddConsole(options =>
{
    options.IncludeScopes = true; // Include scope information in console logs
    options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] "; // Add readable timestamps
});

// Add file logging and debug output in development mode
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddDebug(); // Add debug output for Visual Studio debugging
}

// Configure logging levels for different components
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning); // Reduce ASP.NET Core noise
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning); // Reduce EF noise
builder.Logging.AddFilter("System.Net.Http", LogLevel.Information); // Show HTTP client activity
builder.Logging.AddFilter("RichKid", LogLevel.Debug); // Show all our application logs

// 1. Add Session services for user state management
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session expires after 30 minutes of inactivity
    options.Cookie.HttpOnly = true; // Prevent JavaScript access to session cookie
    options.Cookie.IsEssential = true; // Mark as essential for GDPR compliance
});

// 2. Add Cookie Authentication for user login state
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login"; // Redirect to login page when authentication required
        options.LogoutPath = "/Auth/Logout"; // Handle logout requests
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // Cookie expires after 1 hour
        options.SlidingExpiration = true; // Reset expiration on activity
        options.Cookie.HttpOnly = true; // Prevent JavaScript access
        options.Cookie.IsEssential = true; // Mark as essential for functionality
    });

// 3. Add IHttpContextAccessor for accessing HTTP context in services
builder.Services.AddHttpContextAccessor();

// 4. Add MVC services for controllers and views
builder.Services.AddControllersWithViews();

// 5. Register HttpClient for UserService communication with API
builder.Services.AddHttpClient<UserService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30); // Set reasonable timeout for API calls
    client.DefaultRequestHeaders.Add("User-Agent", "RichKid.Web/1.0"); // Identify our application
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    if (builder.Environment.IsDevelopment())
    {
        // Allow self-signed certificates in development for local HTTPS
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;
    }
    return handler;
});

// 6. Register HttpClient for Auth service with similar configuration
builder.Services.AddHttpClient<AuthService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30); // Set reasonable timeout for auth calls
    client.DefaultRequestHeaders.Add("User-Agent", "RichKid.Web/1.0"); // Identify our application
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    if (builder.Environment.IsDevelopment())
    {
        // Allow self-signed certificates in development for local HTTPS
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;
    }
    return handler;
});

// 7. Register services with dependency injection using shared interfaces
builder.Services.AddScoped<IUserService, UserService>(); // User management service
builder.Services.AddScoped<RichKid.Shared.Services.IAuthService, AuthService>(); // Authentication service

var app = builder.Build();

// Log application startup information for monitoring
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("RichKid Web application starting up in {Environment} mode", app.Environment.EnvironmentName);

// Get API configuration settings for logging
var apiBaseUrl = app.Configuration["ApiSettings:BaseUrl"];
logger.LogInformation("Configured to connect to API at: {ApiBaseUrl}", apiBaseUrl ?? "Not configured");

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    logger.LogInformation("Production mode: Enabling exception handler and HSTS");
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    logger.LogInformation("Development mode: Detailed error pages enabled");
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// 8. Add Authentication and Authorization middleware (ORDER IS CRITICAL!)
app.UseAuthentication();
app.UseSession(); 
app.UseAuthorization();

// Add request logging middleware to track user activity (AFTER session is configured)
app.Use(async (context, next) =>
{
    var requestLogger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var startTime = DateTime.UtcNow;
    
    // Get user information if available (now session is properly configured)
    var userId = context.Session.GetInt32("UserID");
    var userInfo = userId.HasValue ? $"User ID: {userId}" : "Anonymous";
    
    // Log incoming request with user context
    requestLogger.LogInformation("HTTP {Method} {Path} started by {UserInfo} at {StartTime}", 
        context.Request.Method, 
        context.Request.Path, 
        userInfo,
        startTime.ToString("HH:mm:ss.fff"));
    
    await next();
    
    // Log request completion with timing
    var duration = DateTime.UtcNow - startTime;
    requestLogger.LogInformation("HTTP {Method} {Path} completed with {StatusCode} in {Duration}ms for {UserInfo}", 
        context.Request.Method, 
        context.Request.Path, 
        context.Response.StatusCode,
        duration.TotalMilliseconds,
        userInfo);
});

// 9. Configure default route to start at login page
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

logger.LogInformation("RichKid Web application is ready to accept requests");

app.Run();