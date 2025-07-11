using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RichKid.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

// JWT Authentication
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

//  Authorization
builder.Services.AddAuthorization();

// CORS — Allow only your Web domain
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

// Controllers + Swagger/OpenAPI
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();  // Used for Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "RichKid.API", Version = "v1" });

    // Add JWT support to Swagger
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

// Register custom services for dependency injection
builder.Services.AddScoped<IDataService, DataService>();
builder.Services.AddScoped<IUserService, UserService>();

var app = builder.Build();

// ===== Middleware =====
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "RichKid.API V1");
        // c.RoutePrefix = "";   // If you want the UI to load on API root ("/")
    });
}
else
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

app.UseCors("AllowWeb");

app.UseAuthentication();   // Must be before UseAuthorization()
app.UseAuthorization();

app.MapControllers();

app.Run();