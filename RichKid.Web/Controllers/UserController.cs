using Microsoft.AspNetCore.Mvc;
using RichKid.Shared.Models;
using RichKid.Web.Services;
using RichKid.Web.Filters;
using Microsoft.AspNetCore.Authorization;

namespace RichKid.Web.Controllers
{
    [Authorize] // Require authentication for all actions
    public class UserController : Controller
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // All authenticated users can view the user list
        [RequireViewPermission]
        public async Task<IActionResult> Index(string search = "", string status = "")
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();

                if (!string.IsNullOrEmpty(search))
                {
                    users = users.Where(u =>
                        u.UserName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        (u.Data?.Email ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        (u.Data?.Phone ?? "").Contains(search)).ToList();
                }

                if (status == "active") users = users.Where(u => u.Active).ToList();
                else if (status == "inactive") users = users.Where(u => !u.Active).ToList();

                return View(users);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error loading users: {ex.Message}";
                return View(new List<User>());
            }
        }

        // Only admins and editors can create users
        [RequireCreatePermission]
        public IActionResult Create() => View();

        [HttpPost]
        [RequireCreatePermission]
        public async Task<IActionResult> Create(User user)
        {
            if (user.Data == null) user.Data = new UserData();

            if (ModelState.IsValid)
            {
                try
                {
                    await _userService.AddUserAsync(user);
                    TempData["SuccessMessage"] = "User created successfully!";
                    return RedirectToAction("Index");
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"HttpRequestException in Create: {ex.Message}");
                    
                    // Check for specific error messages
                    if (ex.Message.Contains("Username already exists"))
                    {
                        ModelState.AddModelError("UserName", "Username already exists. Please choose a different username.");
                    }
                    else
                    {
                        // Use the clean error message from the API
                        ModelState.AddModelError("", ex.Message);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // User's session expired
                    return RedirectToAction("Login", "Auth");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"General Exception in Create: {ex.GetType().Name} - {ex.Message}");
                    ModelState.AddModelError("", $"Unexpected error: {ex.Message}");
                }
            }
            else
            {
                // Log validation errors for debugging
                Console.WriteLine("ModelState is invalid:");
                foreach (var modelError in ModelState)
                {
                    foreach (var error in modelError.Value.Errors)
                    {
                        Console.WriteLine($"  {modelError.Key}: {error.ErrorMessage}");
                    }
                }
            }

            return View(user);
        }

        // Users can edit themselves, admins and editors can edit anyone
        [RequireEditPermission(allowSelfEdit: true)]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                return user == null ? NotFound() : View(user);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error loading user: {ex.Message}";
                return NotFound();
            }
        }

        [HttpPost]
        [RequireEditPermission(allowSelfEdit: true)]
        public async Task<IActionResult> Edit(User user)
        {
            if (user.Data == null)
                user.Data = new UserData();

            if (ModelState.IsValid)
            {
                try
                {
                    await _userService.UpdateUserAsync(user);
                    TempData["SuccessMessage"] = "User updated successfully!";
                    return RedirectToAction("Index");
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"HttpRequestException in Edit: {ex.Message}");
                    
                    // Check for specific error messages
                    if (ex.Message.Contains("Username already exists"))
                    {
                        ModelState.AddModelError("UserName", "Username already exists. Please choose a different username.");
                    }
                    else
                    {
                        // Use the clean error message from the API
                        ModelState.AddModelError("", ex.Message);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // User's session expired
                    return RedirectToAction("Login", "Auth");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"General Exception in Edit: {ex.GetType().Name} - {ex.Message}");
                    ModelState.AddModelError("", $"Unexpected error: {ex.Message}");
                }
            }
            else
            {
                // Log validation errors for debugging
                Console.WriteLine("ModelState is invalid in Edit:");
                foreach (var modelError in ModelState)
                {
                    foreach (var error in modelError.Value.Errors)
                    {
                        Console.WriteLine($"  {modelError.Key}: {error.ErrorMessage}");
                    }
                }
            }
            
            return View(user);
        }

        // Only admins can delete users
        [RequireDeletePermission]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                return user == null ? NotFound() : View(user);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error loading user: {ex.Message}";
                return NotFound();
            }
        }

        [HttpPost]
        [RequireDeletePermission]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _userService.DeleteUserAsync(id);
                TempData["SuccessMessage"] = "User deleted successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting user: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
    }
}