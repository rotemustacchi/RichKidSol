using Microsoft.AspNetCore.Mvc;
using RichKid.Web.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace RichKid.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;

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
                var result = await _authService.LoginAsync(username, password);
                
                if (result.Success && !string.IsNullOrEmpty(result.Token))
                {
                    // Parse JWT token to extract user information
                    var handler = new JwtSecurityTokenHandler();
                    var jsonToken = handler.ReadJwtToken(result.Token);
                    
                    // Create claims identity for the user
                    var claims = new List<Claim>();
                    
                    foreach (var claim in jsonToken.Claims)
                    {
                        claims.Add(new Claim(claim.Type, claim.Value));
                    }
                    
                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                    
                    // Sign in the user with the claims
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);
                    
                    // Also store in session for backward compatibility
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
                    
                    return RedirectToAction("Index", "User");
                }
                else
                {
                    // Display the specific error message from the API
                    ViewBag.Error = result.ErrorMessage ?? "Login failed";
                    return View();
                }
            }
            catch (UnauthorizedAccessException)
            {
                ViewBag.Error = "Authentication failed";
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Login failed: {ex.Message}";
                return View();
            }
        }

        public async Task<IActionResult> Logout()
        {
            _authService.ClearAuthToken();
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}