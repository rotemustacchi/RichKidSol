using Microsoft.AspNetCore.Mvc;
using RichKid.Web.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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
                    
                    // Extract user information from token claims
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
                    ViewBag.Error = result.ErrorMessage ?? "wrong username or password.";
                    return View();
                }
            }
            catch (UnauthorizedAccessException)
            {
                ViewBag.Error = "wrong username or password.";
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Login failed: {ex.Message}";
                return View();
            }
        }

        public IActionResult Logout()
        {
            _authService.ClearAuthToken();
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}