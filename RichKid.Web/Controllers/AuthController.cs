using Microsoft.AspNetCore.Mvc;
using RichKid.Shared.Services; // Added this to use shared IAuthService interface
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace RichKid.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService; // Now using shared interface

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // If user is already authenticated, redirect to User page
            if (_authService.IsAuthenticated())
            {
                return RedirectToAction("Index", "User");
            }
            
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            try
            {
                // Call the authentication service to validate credentials
                var result = await _authService.LoginAsync(username, password);
                
                if (result.Success && !string.IsNullOrEmpty(result.Token))
                {
                    // Parse JWT token to extract user information for the session
                    var handler = new JwtSecurityTokenHandler();
                    var jsonToken = handler.ReadJwtToken(result.Token);
                    
                    // Create claims identity for cookie authentication
                    var claims = new List<Claim>();
                    
                    // Copy all claims from the JWT token
                    foreach (var claim in jsonToken.Claims)
                    {
                        claims.Add(new Claim(claim.Type, claim.Value));
                    }
                    
                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                    
                    // Sign in the user with the claims for cookie authentication
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);
                    
                    // Also store in session for backward compatibility with existing code
                    var userIdClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == "UserID");
                    var userGroupIdClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == "UserGroupID");
                    
                    if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                    {
                        HttpContext.Session.SetInt32("UserID", userId);
                    }
                    
                    if (userGroupIdClaim != null && int.TryParse(userGroupIdClaim.Value, out int userGroupId))
                    {
                        HttpContext.Session.SetInt32("UserGroupID", userGroupId);
                    }
                    
                    // Redirect to the main user management page
                    return RedirectToAction("Index", "User");
                }
                else
                {
                    // Display the specific error message from the authentication service
                    ViewBag.Error = result.ErrorMessage ?? "Login failed";
                    return View();
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                // Handle authentication failures with user-friendly message
                ViewBag.Error = ex.Message; // Our service provides user-friendly messages
                return View();
            }
            catch (Exception ex)
            {
                // Handle any other unexpected errors during login
                Console.WriteLine($"Unexpected error in login: {ex.Message}");
                ViewBag.Error = "An unexpected error occurred during login. Please try again.";
                return View();
            }
        }

        public async Task<IActionResult> Logout()
        {
            // Clear the authentication token and session data
            _authService.ClearAuthToken();
            HttpContext.Session.Clear();
            
            // Sign out from cookie authentication
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            // Redirect back to login page
            return RedirectToAction("Login");
        }
    }
}