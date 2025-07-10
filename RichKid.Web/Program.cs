var builder = WebApplication.CreateBuilder(args);

// 1. מוסיפים שירותי Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 2. מוסיפים IHttpContextAccessor (דרוש ל־@inject ב־Razor)
builder.Services.AddHttpContextAccessor();

// 3. מוסיפים MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// 4. Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// חשוב: UseRouting לפני ה־Session/Authorization
app.UseRouting();

// 5. מפעילים Session
app.UseSession();

// 6. מפעילים Authorization
app.UseAuthorization();

// 7. מיפוי הנתיבים
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();
