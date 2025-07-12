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
using RichKid.Shared.DTOs;

namespace RichKid.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IUserService _userService;

        public AuthController(IConfiguration config, IUserService userService)
        {
            _config = config;
            _userService = userService;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            // First, check if user exists by username
            var userByUsername = _userService.GetAllUsers()
                .FirstOrDefault(u => u.UserName == request.UserName);

            if (userByUsername == null)
            {
                return Unauthorized("Username not found");
            }

            // Check if password matches
            if (userByUsername.Password != request.Password)
            {
                return Unauthorized("Incorrect password");
            }

            // Check if user is active
            if (!userByUsername.Active)
            {
                return Unauthorized("Account is inactive. Please contact an administrator");
            }

            // User is valid and active, create JWT token
            var jwt = _config.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddMinutes(int.Parse(jwt["DurationInMinutes"]!));

            // Create claims for the token including role-based claims
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userByUsername.UserName),
                new Claim("UserID", userByUsername.UserID.ToString()),
                new Claim("UserGroupID", (userByUsername.UserGroupID ?? 0).ToString())
            };

            // Add role-based claims for authorization using shared constants
            switch (userByUsername.UserGroupID)
            {
                case UserGroups.ADMIN:
                    claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                    claims.Add(new Claim("CanCreate", "true"));
                    claims.Add(new Claim("CanEdit", "true"));
                    claims.Add(new Claim("CanDelete", "true"));
                    claims.Add(new Claim("CanView", "true"));
                    break;
                case UserGroups.EDITOR:
                    claims.Add(new Claim(ClaimTypes.Role, "Editor"));
                    claims.Add(new Claim("CanCreate", "true"));
                    claims.Add(new Claim("CanEdit", "true"));
                    claims.Add(new Claim("CanDelete", "false"));
                    claims.Add(new Claim("CanView", "true"));
                    break;
                case UserGroups.REGULAR_USER:
                    claims.Add(new Claim(ClaimTypes.Role, "User"));
                    claims.Add(new Claim("CanCreate", "false"));
                    claims.Add(new Claim("CanEdit", "self"));
                    claims.Add(new Claim("CanDelete", "false"));
                    claims.Add(new Claim("CanView", "true"));
                    break;
                case UserGroups.VIEW_ONLY:
                    claims.Add(new Claim(ClaimTypes.Role, "Viewer"));
                    claims.Add(new Claim("CanCreate", "false"));
                    claims.Add(new Claim("CanEdit", "self"));
                    claims.Add(new Claim("CanDelete", "false"));
                    claims.Add(new Claim("CanView", "true"));
                    break;
                default:
                    claims.Add(new Claim(ClaimTypes.Role, "Unassigned"));
                    claims.Add(new Claim("CanCreate", "false"));
                    claims.Add(new Claim("CanEdit", "false"));
                    claims.Add(new Claim("CanDelete", "false"));
                    claims.Add(new Claim("CanView", "false"));
                    break;
            }

            // Generate the token
            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            var response = new LoginResponse
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token)
            };

            return Ok(response);
        }
    }
}