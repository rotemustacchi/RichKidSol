using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RichKid.Shared.Services;
using RichKid.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure comprehensive logging with multiple providers
builder.Logging.ClearProviders(); // Start fresh with no default providers
builder.Logging.AddConsole(options =>
{
    options.IncludeScopes = true; // Include scope information in console logs
    options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] "; // Add readable timestamps
});

// Add file logging if in development mode for persistent log storage
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddDebug(); // Add debug output for Visual Studio debugging
}

// Configure detailed logging levels for different components
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning); // Reduce ASP.NET Core noise
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Information); // Show EF queries
builder.Logging.AddFilter("RichKid", LogLevel.Debug); // Show all our application logs
builder.Logging.AddFilter("System.Net.Http", LogLevel.Information); // Show HTTP client activity

// JWT Authentication setup with enhanced logging
builder.Services
  .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(options =>
  {
    var jwt = builder.Configuration.GetSection("Jwt");
    options.TokenValidationParameters = new TokenValidationParameters
    {
      ValidateIssuer           = true,
      ValidateAudience         = true,
      ValidateLifetime         = true,
      ValidateIssuerSigningKey = true,
      ValidIssuer              = jwt["Issuer"],
      ValidAudience            = jwt["Audience"],
      IssuerSigningKey         = new SymmetricSecurityKey(
                                  Encoding.UTF8.GetBytes(jwt["Key"]!))
    };
    
    // Add JWT event handlers for logging authentication activities
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILogger<Program>>();
            logger.LogWarning("JWT Authentication failed: {Error}", context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILogger<Program>>();
            var userId = context.Principal?.FindFirst("UserID")?.Value ?? "Unknown";
            logger.LogInformation("JWT token validated successfully for user ID: {UserId}", userId);
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILogger<Program>>();
            logger.LogWarning("JWT authentication challenge triggered: {Error}", context.Error);
            return Task.CompletedTask;
        }
    };
  });

// Authorization with JWT-based policies - no changes to existing logic
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanView", policy =>
        policy.RequireClaim("CanView", "true"));
    
    options.AddPolicy("CanCreate", policy =>
        policy.RequireClaim("CanCreate", "true"));
    
    options.AddPolicy("CanDelete", policy =>
        policy.RequireClaim("CanDelete", "true"));
});

// CORS configuration - Allow only your Web domain (no changes)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWeb", policy =>
    {
        policy
          .WithOrigins("https://localhost:7143")  // RichKid.Web address
          .AllowAnyHeader()
          .AllowAnyMethod();
    });
});

// Controllers + Swagger/OpenAPI configuration
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "RichKid.API", Version = "v1" });

    // Add JWT support to Swagger - no changes to existing logic
    c.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter token here: Bearer {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Register custom services for dependency injection - maintaining existing architecture
builder.Services.AddScoped<IDataService, DataService>();
builder.Services.AddScoped<IUserService, UserService>();

var app = builder.Build();

// Log application startup information for monitoring
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("RichKid API starting up in {Environment} mode", app.Environment.EnvironmentName);
logger.LogInformation("JWT Issuer configured as: {Issuer}", 
    app.Configuration.GetSection("Jwt")["Issuer"]);

// ===== Middleware Pipeline =====
if (app.Environment.IsDevelopment())
{
    logger.LogInformation("Development mode: Enabling Swagger UI");
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "RichKid.API V1");
    });
}
else
{
    logger.LogInformation("Production mode: HTTPS redirection and HSTS enabled");
    app.UseHttpsRedirection();
    app.UseHsts();
}

app.UseCors("AllowWeb");

// Add request logging middleware to track all incoming requests
app.Use(async (context, next) =>
{
    var requestLogger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var startTime = DateTime.UtcNow;
    
    // Log incoming request details
    requestLogger.LogInformation("HTTP {Method} {Path} started at {StartTime}", 
        context.Request.Method, 
        context.Request.Path, 
        startTime.ToString("HH:mm:ss.fff"));
    
    await next();
    
    // Log request completion with timing information
    var duration = DateTime.UtcNow - startTime;
    requestLogger.LogInformation("HTTP {Method} {Path} completed with {StatusCode} in {Duration}ms", 
        context.Request.Method, 
        context.Request.Path, 
        context.Response.StatusCode,
        duration.TotalMilliseconds);
});

app.UseAuthentication();   
app.UseAuthorization();

app.MapControllers();

logger.LogInformation("RichKid API is ready to accept requests on {Urls}", 
    string.Join(", ", app.Urls));

app.Run();

public partial class Program { }