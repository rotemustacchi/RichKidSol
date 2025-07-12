using Microsoft.AspNetCore.Mvc;
using RichKid.Shared.Services; // Use shared IAuthService interface
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace RichKid.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService; // Use shared interface for authentication
        private readonly ILogger<AuthController> _logger; // Add logger for tracking authentication flow

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger; // Inject logger to monitor authentication activities
        }

        [HttpGet]
        public IActionResult Login()
        {
            _logger.LogDebug("Login page requested");
            
            // If user is already authenticated, redirect to User management page
            if (_authService.IsAuthenticated())
            {
                _logger.LogInformation("User already authenticated, redirecting to User management page");
                return RedirectToAction("Index", "User");
            }
            
            _logger.LogDebug("Displaying login page for unauthenticated user");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            try
            {
                // Log login attempt start (without sensitive information)
                _logger.LogInformation("Login attempt started for username: {Username} from IP: {ClientIP}", 
                    username, HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown");
                
                // Validate input parameters
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    _logger.LogWarning("Login attempt with empty credentials for username: {Username}", username ?? "null");
                    ViewBag.Error = "Username and password are required";
                    return View();
                }

                // Call the authentication service to validate credentials
                _logger.LogDebug("Calling authentication service for user: {Username}", username);
                var result = await _authService.LoginAsync(username, password);
                
                if (result.Success && !string.IsNullOrEmpty(result.Token))
                {
                    _logger.LogInformation("Authentication successful for user: {Username}", username);
                    
                    // Parse JWT token to extract user information for session management
                    _logger.LogDebug("Parsing JWT token for user: {Username}", username);
                    var handler = new JwtSecurityTokenHandler();
                    var jsonToken = handler.ReadJwtToken(result.Token);
                    
                    // Create claims identity for cookie authentication
                    var claims = new List<Claim>();
                    
                    // Copy all claims from the JWT token to the cookie
                    foreach (var claim in jsonToken.Claims)
                    {
                        claims.Add(new Claim(claim.Type, claim.Value));
                    }
                    
                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                    
                    // Sign in the user with cookie authentication
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);
                    _logger.LogDebug("Cookie authentication established for user: {Username}", username);
                    
                    // Also store in session for backward compatibility with existing code
                    var userIdClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == "UserID");
                    var userGroupIdClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == "UserGroupID");
                    
                    int? userId = null; // Initialize userId to avoid compilation error
                    if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int parsedUserId))
                    {
                        userId = parsedUserId; // Store the parsed value
                        HttpContext.Session.SetInt32("UserID", parsedUserId);
                        _logger.LogDebug("Set session UserID: {UserId} for user: {Username}", parsedUserId, username);
                    }
                    
                    if (userGroupIdClaim != null && int.TryParse(userGroupIdClaim.Value, out int userGroupId))
                    {
                        HttpContext.Session.SetInt32("UserGroupID", userGroupId);
                        var groupName = RichKid.Shared.Models.UserGroups.GetName(userGroupId, useEnglish: true);
                        _logger.LogDebug("Set session UserGroupID: {GroupId} ({GroupName}) for user: {Username}", 
                            userGroupId, groupName, username);
                    }
                    
                    _logger.LogInformation("Login completed successfully for user: {Username} (ID: {UserId}), redirecting to User management", 
                        username, userId?.ToString() ?? "Unknown");
                    
                    // Redirect to the main user management page
                    return RedirectToAction("Index", "User");
                }
                else
                {
                    // Log failed authentication with specific reason
                    var errorMessage = result.ErrorMessage ?? "Login failed";
                    _logger.LogWarning("Login failed for user: {Username}. Reason: {ErrorMessage}", username, errorMessage);
                    
                    // Display the specific error message from the authentication service
                    ViewBag.Error = errorMessage;
                    return View();
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                // Handle authentication failures with user-friendly message
                _logger.LogWarning("Authentication exception for user: {Username}. Message: {ErrorMessage}", 
                    username, ex.Message);
                ViewBag.Error = ex.Message; // Our service provides user-friendly messages
                return View();
            }
            catch (HttpRequestException ex)
            {
                // Handle API communication errors
                _logger.LogError(ex, "HTTP error during login for user: {Username}", username);
                ViewBag.Error = "Unable to connect to authentication service. Please try again.";
                return View();
            }
            catch (Exception ex)
            {
                // Handle any other unexpected errors during login
                _logger.LogError(ex, "Unexpected error during login for user: {Username}", username);
                ViewBag.Error = "An unexpected error occurred during login. Please try again.";
                return View();
            }
        }

        public async Task<IActionResult> Logout()
        {
            try
            {
                // Get user information before clearing session
                var userId = HttpContext.Session.GetInt32("UserID");
                var userName = User.Identity?.Name ?? "Unknown";
                
                _logger.LogInformation("Logout started for user: {UserName} (ID: {UserId})", userName, userId);
                
                // Clear the authentication token and session data
                _authService.ClearAuthToken();
                HttpContext.Session.Clear();
                
                // Sign out from cookie authentication
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                
                _logger.LogInformation("Logout completed successfully for user: {UserName} (ID: {UserId})", userName, userId);
                
                // Redirect back to login page
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                // Log errors during logout process
                _logger.LogError(ex, "Error occurred during logout process");
                
                // Even if logout has errors, still redirect to login page
                return RedirectToAction("Login");
            }
        }
    }
}