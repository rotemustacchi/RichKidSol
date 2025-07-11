using RichKid.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Session services for user state management
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);      // Session expires after 30 minutes of inactivity
    options.Cookie.HttpOnly = true;                      // Prevent client-side JavaScript access to session cookie
    options.Cookie.IsEssential = true;                   // Required for GDPR compliance
});

// 2. Add IHttpContextAccessor (required for @inject in Razor pages)
builder.Services.AddHttpContextAccessor();

// 3. Add MVC services
builder.Services.AddControllersWithViews();

// 4. Register HttpClient for API communication
builder.Services.AddHttpClient<IUserService, UserService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "RichKid.Web/1.0");
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    if (builder.Environment.IsDevelopment())
    {
        // Ignore SSL certificate errors in development
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;
    }
    return handler;
});

// 5. Register HttpClient for Auth service
builder.Services.AddHttpClient<IAuthService, AuthService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "RichKid.Web/1.0");
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    if (builder.Environment.IsDevelopment())
    {
        // Ignore SSL certificate errors in development
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;
    }
    return handler;
});

// 6. Register services with dependency injection
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");              // Handle exceptions in production
    app.UseHsts();                                       // HTTP Strict Transport Security
}

app.UseHttpsRedirection();                               // Redirect HTTP to HTTPS
app.UseStaticFiles();                                    // Serve static files (CSS, JS, images)

// Important: UseRouting must come before Session/Authorization
app.UseRouting();

// 7. Enable Session middleware
app.UseSession();

// 8. Enable Authorization middleware
app.UseAuthorization();

// 9. Configure default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");  // Default to Auth controller, Login action

app.Run();