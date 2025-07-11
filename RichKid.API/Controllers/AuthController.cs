using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RichKid.Shared.Models;
using RichKid.Shared.Services;

namespace RichKid.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IUserService _userService;

        // Constructor with dependency injection
        public AuthController(IConfiguration config, IUserService userService)
        {
            _config = config;
            _userService = userService;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginModel body)
        {
            // Find user with matching credentials and active status
            var user = _userService.GetAllUsers()
                .FirstOrDefault(u =>
                    u.UserName == body.UserName &&
                    u.Password == body.Password &&
                    u.Active);
            
            if (user == null)
                return Unauthorized("Username/password are incorrect or user is not active.");

            // Create JWT token
            var jwt = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddMinutes(int.Parse(jwt["DurationInMinutes"]!));

            // Create claims for the token
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim("UserID", user.UserID.ToString()),
                new Claim("UserGroupID", (user.UserGroupID ?? 0).ToString())
            };

            // Generate the token
            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
        }
    }

    public class LoginModel
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}