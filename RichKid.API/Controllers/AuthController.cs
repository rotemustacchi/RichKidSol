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
        private readonly ILogger<AuthController> _logger; // Add logger for tracking authentication activities

        public AuthController(IConfiguration config, IUserService userService, ILogger<AuthController> logger)
        {
            _config = config;
            _userService = userService;
            _logger = logger; // Inject logger to track all authentication operations
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            // Log the start of a login attempt (without sensitive information)
            _logger.LogInformation("Login attempt started for username: {Username}", request.UserName);
            
            try
            {
                // First step: check if user exists by username
                _logger.LogDebug("Looking up user by username: {Username}", request.UserName);
                var userByUsername = _userService.GetAllUsers()
                    .FirstOrDefault(u => u.UserName == request.UserName);

                if (userByUsername == null)
                {
                    // Log failed login due to non-existent username
                    _logger.LogWarning("Login failed: Username not found - {Username}", request.UserName);
                    return Unauthorized("Username not found");
                }

                _logger.LogDebug("User found: {Username}, checking password...", request.UserName);

                // Second step: verify the password matches
                if (userByUsername.Password != request.Password)
                {
                    // Log failed login due to incorrect password (don't log the actual password)
                    _logger.LogWarning("Login failed: Incorrect password for username - {Username}", request.UserName);
                    return Unauthorized("Incorrect password");
                }

                _logger.LogDebug("Password verified for user: {Username}, checking active status...", request.UserName);

                // Third step: ensure the user account is active
                if (!userByUsername.Active)
                {
                    // Log failed login due to inactive account
                    _logger.LogWarning("Login failed: Account inactive for username - {Username}", request.UserName);
                    return Unauthorized("Account is inactive. Please contact an administrator");
                }

                // User credentials are valid and account is active - proceed with JWT creation
                _logger.LogInformation("User validation successful for: {Username} (ID: {UserId})", 
                    request.UserName, userByUsername.UserID);

                // Retrieve JWT configuration settings
                var jwt = _config.GetSection("Jwt");
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var expires = DateTime.Now.AddMinutes(int.Parse(jwt["DurationInMinutes"]!));

                _logger.LogDebug("Creating JWT token for user: {Username}, expires at: {ExpirationTime}", 
                    request.UserName, expires);

                // Build JWT claims based on user information and group permissions
                var claims = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub, userByUsername.UserName),
                    new Claim("UserID", userByUsername.UserID.ToString()),
                    new Claim("UserGroupID", (userByUsername.UserGroupID ?? 0).ToString())
                };

                // Add role-based permission claims based on user group
                var groupName = UserGroups.GetName(userByUsername.UserGroupID, useEnglish: true);
                _logger.LogDebug("Assigning permissions for user: {Username}, group: {GroupName} (ID: {GroupId})", 
                    request.UserName, groupName, userByUsername.UserGroupID);

                switch (userByUsername.UserGroupID)
                {
                    case UserGroups.ADMIN:
                        // Admin users get full system access
                        claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                        claims.Add(new Claim("CanCreate", "true"));
                        claims.Add(new Claim("CanEdit", "true"));
                        claims.Add(new Claim("CanDelete", "true"));
                        claims.Add(new Claim("CanView", "true"));
                        _logger.LogDebug("Admin permissions granted to user: {Username}", request.UserName);
                        break;
                        
                    case UserGroups.EDITOR:
                        // Editors can create and modify but not delete
                        claims.Add(new Claim(ClaimTypes.Role, "Editor"));
                        claims.Add(new Claim("CanCreate", "true"));
                        claims.Add(new Claim("CanEdit", "true"));
                        claims.Add(new Claim("CanDelete", "false"));
                        claims.Add(new Claim("CanView", "true"));
                        _logger.LogDebug("Editor permissions granted to user: {Username}", request.UserName);
                        break;
                        
                    case UserGroups.REGULAR_USER:
                        // Regular users can only edit their own profile
                        claims.Add(new Claim(ClaimTypes.Role, "User"));
                        claims.Add(new Claim("CanCreate", "false"));
                        claims.Add(new Claim("CanEdit", "self"));
                        claims.Add(new Claim("CanDelete", "false"));
                        claims.Add(new Claim("CanView", "true"));
                        _logger.LogDebug("Regular user permissions granted to user: {Username}", request.UserName);
                        break;
                        
                    case UserGroups.VIEW_ONLY:
                        // View-only users can see data but can edit only their own profile
                        claims.Add(new Claim(ClaimTypes.Role, "Viewer"));
                        claims.Add(new Claim("CanCreate", "false"));
                        claims.Add(new Claim("CanEdit", "self"));
                        claims.Add(new Claim("CanDelete", "false"));
                        claims.Add(new Claim("CanView", "true"));
                        _logger.LogDebug("View-only permissions granted to user: {Username}", request.UserName);
                        break;
                        
                    default:
                        // Unassigned users get minimal permissions
                        claims.Add(new Claim(ClaimTypes.Role, "Unassigned"));
                        claims.Add(new Claim("CanCreate", "false"));
                        claims.Add(new Claim("CanEdit", "false"));
                        claims.Add(new Claim("CanDelete", "false"));
                        claims.Add(new Claim("CanView", "false"));
                        _logger.LogWarning("Unassigned group for user: {Username}, minimal permissions granted", 
                            request.UserName);
                        break;
                }

                // Generate the JWT token with all claims
                var token = new JwtSecurityToken(
                    issuer: jwt["Issuer"],
                    audience: jwt["Audience"],
                    claims: claims,
                    expires: expires,
                    signingCredentials: creds
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                // Log successful login completion
                _logger.LogInformation("Login successful for user: {Username} (ID: {UserId}), token expires: {ExpirationTime}", 
                    request.UserName, userByUsername.UserID, expires);

                var response = new LoginResponse
                {
                    Token = tokenString
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                // Log any unexpected errors during the login process
                _logger.LogError(ex, "Unexpected error occurred during login for username: {Username}", request.UserName);
                
                // Return generic error message to avoid exposing internal details
                return StatusCode(500, "An unexpected error occurred during login. Please try again.");
            }
        }
    }
}