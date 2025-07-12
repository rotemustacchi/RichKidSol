using Microsoft.AspNetCore.Mvc;
using RichKid.Shared.Models;
using RichKid.Shared.Services; // Added this to use the shared IUserService interface
using RichKid.Web.Services; // Keep this for any web-specific services if needed
using RichKid.Web.Filters;
using Microsoft.AspNetCore.Authorization;

namespace RichKid.Web.Controllers
{
    [Authorize] // Require authentication for all actions in this controller
    public class UserController : Controller
    {
        private readonly IUserService _userService; // Now using shared interface

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
                // Use the async version for better web performance
                var users = await ((UserService)_userService).GetAllUsersAsync();

                // Apply search filter if provided
                if (!string.IsNullOrEmpty(search))
                {
                    users = users.Where(u =>
                        u.UserName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        (u.Data?.Email ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        (u.Data?.Phone ?? "").Contains(search)).ToList();
                }

                // Apply status filter if provided
                if (status == "active") users = users.Where(u => u.Active).ToList();
                else if (status == "inactive") users = users.Where(u => !u.Active).ToList();

                return View(users);
            }
            catch (Exception ex)
            {
                // Show user-friendly error message
                ViewBag.Error = ex.Message; // Our service now provides user-friendly messages
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
            // Initialize user data if not provided
            if (user.Data == null) user.Data = new UserData();

            if (ModelState.IsValid)
            {
                try
                {
                    // Use async version for better performance
                    await ((UserService)_userService).AddUserAsync(user);
                    TempData["SuccessMessage"] = "User created successfully!";
                    return RedirectToAction("Index");
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"HttpRequestException in Create: {ex.Message}");
                    
                    // Check for specific error messages from our improved service
                    if (ex.Message.Contains("Username already exists") || 
                        ex.Message.Contains("already exists") ||
                        ex.Message.Contains("Username conflict"))
                    {
                        ModelState.AddModelError("UserName", "This username is already taken. Please choose a different username.");
                    }
                    else
                    {
                        // Use the user-friendly error message from our service
                        ModelState.AddModelError("", ex.Message);
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    // User's session expired - show friendly message
                    ViewBag.Error = ex.Message; // Our service provides user-friendly auth messages
                    return RedirectToAction("Login", "Auth");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"General Exception in Create: {ex.GetType().Name} - {ex.Message}");
                    // Our service now provides user-friendly error messages
                    ModelState.AddModelError("", ex.Message);
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
                // Use async version for better performance
                var user = await ((UserService)_userService).GetUserByIdAsync(id);
                return user == null ? NotFound() : View(user);
            }
            catch (Exception ex)
            {
                // Show user-friendly error message from our improved service
                ViewBag.Error = ex.Message;
                return NotFound();
            }
        }

        [HttpPost]
        [RequireEditPermission(allowSelfEdit: true)]
        public async Task<IActionResult> Edit(User user)
        {
            // Initialize user data if not provided
            if (user.Data == null)
                user.Data = new UserData();

            if (ModelState.IsValid)
            {
                try
                {
                    Console.WriteLine($"=== Controller.Edit - About to call UpdateUserAsync ===");
                    Console.WriteLine($"User ID: {user.UserID}, Username: {user.UserName}");
                    
                    // Use async version for better performance
                    await ((UserService)_userService).UpdateUserAsync(user);
                    TempData["SuccessMessage"] = "User updated successfully!";
                    return RedirectToAction("Index");
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"=== Controller.Edit - Caught HttpRequestException ===");
                    Console.WriteLine($"Exception message: {ex.Message}");
                    Console.WriteLine($"Exception type: {ex.GetType().Name}");
                    
                    // Check for specific error messages from our improved service
                    if (ex.Message.Contains("Username already exists") || 
                        ex.Message.Contains("already exists") ||
                        ex.Message.Contains("Username conflict"))
                    {
                        Console.WriteLine("=== Detected username conflict - adding model error ===");
                        ModelState.AddModelError("UserName", "This username is already taken. Please choose a different username.");
                    }
                    else
                    {
                        // Use the user-friendly error message from our service
                        ModelState.AddModelError("", ex.Message);
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine($"=== Controller.Edit - Caught UnauthorizedAccessException ===");
                    Console.WriteLine($"Exception message: {ex.Message}");
                    // User's session expired - show friendly message
                    ViewBag.Error = ex.Message; // Our service provides user-friendly auth messages
                    return RedirectToAction("Login", "Auth");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"=== Controller.Edit - Caught General Exception ===");
                    Console.WriteLine($"Exception type: {ex.GetType().Name}");
                    Console.WriteLine($"Exception message: {ex.Message}");
                    Console.WriteLine($"Inner exception: {ex.InnerException?.Message ?? "None"}");
                    // Our service now provides user-friendly error messages
                    ModelState.AddModelError("", ex.Message);
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
                // Use async version for better performance
                var user = await ((UserService)_userService).GetUserByIdAsync(id);
                return user == null ? NotFound() : View(user);
            }
            catch (Exception ex)
            {
                // Show user-friendly error message from our improved service
                ViewBag.Error = ex.Message;
                return NotFound();
            }
        }

        [HttpPost]
        [RequireDeletePermission]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                // Use async version for better performance
                await ((UserService)_userService).DeleteUserAsync(id);
                TempData["SuccessMessage"] = "User deleted successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Show user-friendly error message from our improved service
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Index");
            }
        }
    }
}