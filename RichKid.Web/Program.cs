using RichKid.Web.Services;
using RichKid.Shared.Services; // Added this to use shared interfaces
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Session services for user state management
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 2. Add Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

// 3. Add IHttpContextAccessor
builder.Services.AddHttpContextAccessor();

// 4. Add MVC services
builder.Services.AddControllersWithViews();

// 5. Register HttpClient for UserService communication with API
builder.Services.AddHttpClient<UserService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "RichKid.Web/1.0");
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    if (builder.Environment.IsDevelopment())
    {
        // Allow self-signed certificates in development
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;
    }
    return handler;
});

// 6. Register HttpClient for Auth service
builder.Services.AddHttpClient<AuthService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "RichKid.Web/1.0");
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    if (builder.Environment.IsDevelopment())
    {
        // Allow self-signed certificates in development
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;
    }
    return handler;
});

// 7. Register services with dependency injection using shared interfaces
builder.Services.AddScoped<IUserService, UserService>(); // Using shared interface
builder.Services.AddScoped<RichKid.Shared.Services.IAuthService, AuthService>(); // Using shared interface

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// 8. Add Authentication and Authorization middleware
app.UseAuthentication();
app.UseSession();
app.UseAuthorization();

// 9. Configure default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();