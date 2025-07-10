using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;


var builder = WebApplication.CreateBuilder(args);

// 1️⃣ JWT Authentication
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
  });

// 2️⃣ Authorization
builder.Services.AddAuthorization();

// 3️⃣ CORS — לאפשר רק לדומיין של ה-Web שלך
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWeb", policy =>
    {
        policy
          .WithOrigins("https://localhost:7143")  // כתובת RichKid.Web שלך
          .AllowAnyHeader()
          .AllowAnyMethod();
    });
});

// 4️⃣ Controllers + Swagger/OpenAPI
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();  // משמש ל-Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "RichKid.API", Version = "v1" });

    // הוספת אפשרות ל-JWT
    c.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "הכנס את הטוקן כאן: Bearer {token}"
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


var app = builder.Build();

// ===== Middleware =====
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "RichKid.API V1");
        // c.RoutePrefix = "";   // אם תרצה שה־UI יטען על שורש ה־API ("/")
    });

}
else
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

app.UseCors("AllowWeb");

app.UseAuthentication();   // חייב לפני UseAuthorization()
app.UseAuthorization();

app.MapControllers();

app.Run();
