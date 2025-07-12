using Microsoft.AspNetCore.Mvc;
using RichKid.Shared.Models;
using RichKid.Shared.Services; // Use shared IUserService interface
using RichKid.Web.Services; // Keep this for web-specific services if needed
using RichKid.Web.Filters;
using Microsoft.AspNetCore.Authorization;

namespace RichKid.Web.Controllers
{
    [Authorize] // Require authentication for all actions in this controller
    public class UserController : Controller
    {
        private readonly IUserService _userService; // Use shared interface for consistency
        private readonly ILogger<UserController> _logger; // Add logger for tracking user operations

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger; // Inject logger to monitor all user management activities
        }

        // All authenticated users can view the user list
        [RequireViewPermission]
        public async Task<IActionResult> Index(string search = "", string status = "")
        {
            try
            {
                // Get current user information for logging
                var currentUserId = HttpContext.Session.GetInt32("UserID") ?? 0;
                var currentUserGroupId = HttpContext.Session.GetInt32("UserGroupID") ?? 0;
                
                _logger.LogInformation("User list requested by User ID: {UserId}, Group: {GroupId}, Search: '{Search}', Status: '{Status}'", 
                    currentUserId, currentUserGroupId, search ?? "none", status ?? "none");
                
                // Use the async version for better web performance
                var users = await ((UserService)_userService).GetAllUsersAsync();
                var originalCount = users.Count;
                
                _logger.LogDebug("Retrieved {UserCount} users from service", originalCount);

                // Apply search filter if provided
                if (!string.IsNullOrEmpty(search))
                {
                    users = users.Where(u =>
                        u.UserName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        (u.Data?.Email ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        (u.Data?.Phone ?? "").Contains(search)).ToList();
                    
                    _logger.LogDebug("Applied search filter '{Search}': {FilteredCount} users match", search, users.Count);
                }

                // Apply status filter if provided
                if (status == "active") 
                {
                    users = users.Where(u => u.Active).ToList();
                    _logger.LogDebug("Applied active status filter: {FilteredCount} users", users.Count);
                }
                else if (status == "inactive") 
                {
                    users = users.Where(u => !u.Active).ToList();
                    _logger.LogDebug("Applied inactive status filter: {FilteredCount} users", users.Count);
                }

                _logger.LogInformation("User list displayed successfully for User ID: {UserId}. Showing {DisplayedCount} of {TotalCount} users", 
                    currentUserId, users.Count, originalCount);

                return View(users);
            }
            catch (Exception ex)
            {
                // Log error with context information
                var currentUserId = HttpContext.Session.GetInt32("UserID") ?? 0;
                _logger.LogError(ex, "Error loading user list for User ID: {UserId}", currentUserId);
                
                // Show user-friendly error message
                ViewBag.Error = ex.Message; // Our service now provides user-friendly messages
                return View(new List<User>());
            }
        }

        // Only admins and editors can create users
        [RequireCreatePermission]
        public IActionResult Create() 
        {
            var currentUserId = HttpContext.Session.GetInt32("UserID") ?? 0;
            _logger.LogInformation("Create user page requested by User ID: {UserId}", currentUserId);
            
            return View();
        }

        [HttpPost]
        [RequireCreatePermission]
        public async Task<IActionResult> Create(User user)
        {
            var currentUserId = HttpContext.Session.GetInt32("UserID") ?? 0;
            
            try
            {
                _logger.LogInformation("Create user request started by User ID: {UserId} for new user: {NewUserName}", 
                    currentUserId, user.UserName);
                
                // Initialize user data if not provided
                if (user.Data == null) 
                {
                    user.Data = new UserData();
                    _logger.LogDebug("Initialized empty user data for new user: {NewUserName}", user.UserName);
                }

                if (ModelState.IsValid)
                {
                    _logger.LogDebug("Model validation passed for new user: {NewUserName}", user.UserName);
                    
                    // Use async version for better performance
                    await ((UserService)_userService).AddUserAsync(user);
                    
                    _logger.LogInformation("User created successfully: {NewUserName} (ID: {NewUserId}) by User ID: {UserId}", 
                        user.UserName, user.UserID, currentUserId);
                    
                    TempData["SuccessMessage"] = "User created successfully!";
                    return RedirectToAction("Index");
                }
                else
                {
                    // Log validation errors for debugging
                    var validationErrors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value.Errors)
                        .Select(x => x.ErrorMessage)
                        .ToList();
                    
                    _logger.LogWarning("Model validation failed for new user: {NewUserName} by User ID: {UserId}. Errors: {ValidationErrors}", 
                        user.UserName, currentUserId, string.Join(", ", validationErrors));
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning("HTTP error during user creation for {NewUserName} by User ID: {UserId}. Error: {ErrorMessage}", 
                    user.UserName, currentUserId, ex.Message);
                
                // Check for specific error messages from our improved service
                if (ex.Message.Contains("Username already exists") || 
                    ex.Message.Contains("already exists") ||
                    ex.Message.Contains("Username conflict"))
                {
                    _logger.LogDebug("Username conflict detected for: {NewUserName}", user.UserName);
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
                _logger.LogWarning("Authorization error during user creation by User ID: {UserId}. Error: {ErrorMessage}", 
                    currentUserId, ex.Message);
                
                // User's session expired - show friendly message
                ViewBag.Error = ex.Message; // Our service provides user-friendly auth messages
                return RedirectToAction("Login", "Auth");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during user creation for {NewUserName} by User ID: {UserId}", 
                    user.UserName, currentUserId);
                
                // Our service now provides user-friendly error messages
                ModelState.AddModelError("", ex.Message);
            }

            return View(user);
        }

        // Users can edit themselves, admins and editors can edit anyone
        [RequireEditPermission(allowSelfEdit: true)]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var currentUserId = HttpContext.Session.GetInt32("UserID") ?? 0;
                
                _logger.LogInformation("Edit user page requested by User ID: {UserId} for target user ID: {TargetUserId}", 
                    currentUserId, id);
                
                // Use async version for better performance
                var user = await ((UserService)_userService).GetUserByIdAsync(id);
                
                if (user == null)
                {
                    _logger.LogWarning("Edit user failed - User not found with ID: {TargetUserId}, requested by User ID: {UserId}", 
                        id, currentUserId);
                    return NotFound();
                }
                
                _logger.LogInformation("Edit user page loaded successfully for {TargetUserName} (ID: {TargetUserId}) by User ID: {UserId}", 
                    user.UserName, id, currentUserId);
                
                return View(user);
            }
            catch (Exception ex)
            {
                var currentUserId = HttpContext.Session.GetInt32("UserID") ?? 0;
                _logger.LogError(ex, "Error loading edit user page for ID: {TargetUserId} by User ID: {UserId}", 
                    id, currentUserId);
                
                // Show user-friendly error message from our improved service
                ViewBag.Error = ex.Message;
                return NotFound();
            }
        }

        [HttpPost]
        [RequireEditPermission(allowSelfEdit: true)]
        public async Task<IActionResult> Edit(User user)
        {
            var currentUserId = HttpContext.Session.GetInt32("UserID") ?? 0;
            
            try
            {
                _logger.LogInformation("Edit user request started by User ID: {UserId} for target user: {TargetUserName} (ID: {TargetUserId})", 
                    currentUserId, user.UserName, user.UserID);
                
                // Initialize user data if not provided
                if (user.Data == null)
                {
                    user.Data = new UserData();
                    _logger.LogDebug("Initialized empty user data for user: {TargetUserName}", user.UserName);
                }

                if (ModelState.IsValid)
                {
                    _logger.LogDebug("Model validation passed for user: {TargetUserName} (ID: {TargetUserId})", 
                        user.UserName, user.UserID);
                    
                    // Use async version for better performance
                    await ((UserService)_userService).UpdateUserAsync(user);
                    
                    _logger.LogInformation("User updated successfully: {TargetUserName} (ID: {TargetUserId}) by User ID: {UserId}", 
                        user.UserName, user.UserID, currentUserId);
                    
                    TempData["SuccessMessage"] = "User updated successfully!";
                    return RedirectToAction("Index");
                }
                else
                {
                    // Log validation errors for debugging
                    var validationErrors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value.Errors)
                        .Select(x => x.ErrorMessage)
                        .ToList();
                    
                    _logger.LogWarning("Model validation failed for user: {TargetUserName} (ID: {TargetUserId}) by User ID: {UserId}. Errors: {ValidationErrors}", 
                        user.UserName, user.UserID, currentUserId, string.Join(", ", validationErrors));
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning("HTTP error during user update for {TargetUserName} (ID: {TargetUserId}) by User ID: {UserId}. Error: {ErrorMessage}", 
                    user.UserName, user.UserID, currentUserId, ex.Message);
                
                // Check for specific error messages from our improved service
                if (ex.Message.Contains("Username already exists") || 
                    ex.Message.Contains("already exists") ||
                    ex.Message.Contains("Username conflict"))
                {
                    _logger.LogDebug("Username conflict detected during update for: {TargetUserName}", user.UserName);
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
                _logger.LogWarning("Authorization error during user update by User ID: {UserId} for target user ID: {TargetUserId}. Error: {ErrorMessage}", 
                    currentUserId, user.UserID, ex.Message);
                
                // User's session expired - show friendly message
                ViewBag.Error = ex.Message; // Our service provides user-friendly auth messages
                return RedirectToAction("Login", "Auth");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during user update for {TargetUserName} (ID: {TargetUserId}) by User ID: {UserId}", 
                    user.UserName, user.UserID, currentUserId);
                
                // Our service now provides user-friendly error messages
                ModelState.AddModelError("", ex.Message);
            }
            
            return View(user);
        }

        // Only admins can delete users
        [RequireDeletePermission]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var currentUserId = HttpContext.Session.GetInt32("UserID") ?? 0;
                
                _logger.LogInformation("Delete user page requested by User ID: {UserId} for target user ID: {TargetUserId}", 
                    currentUserId, id);
                
                // Use async version for better performance
                var user = await ((UserService)_userService).GetUserByIdAsync(id);
                
                if (user == null)
                {
                    _logger.LogWarning("Delete user failed - User not found with ID: {TargetUserId}, requested by User ID: {UserId}", 
                        id, currentUserId);
                    return NotFound();
                }
                
                _logger.LogInformation("Delete user page loaded successfully for {TargetUserName} (ID: {TargetUserId}) by User ID: {UserId}", 
                    user.UserName, id, currentUserId);
                
                return View(user);
            }
            catch (Exception ex)
            {
                var currentUserId = HttpContext.Session.GetInt32("UserID") ?? 0;
                _logger.LogError(ex, "Error loading delete user page for ID: {TargetUserId} by User ID: {UserId}", 
                    id, currentUserId);
                
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
                var currentUserId = HttpContext.Session.GetInt32("UserID") ?? 0;
                
                _logger.LogInformation("Delete user confirmation received by User ID: {UserId} for target user ID: {TargetUserId}", 
                    currentUserId, id);
                
                // Get user info before deletion for logging
                var userToDelete = await ((UserService)_userService).GetUserByIdAsync(id);
                var userNameToDelete = userToDelete?.UserName ?? "Unknown";
                
                // Use async version for better performance
                await ((UserService)_userService).DeleteUserAsync(id);
                
                _logger.LogInformation("User deleted successfully: {DeletedUserName} (ID: {TargetUserId}) by User ID: {UserId}", 
                    userNameToDelete, id, currentUserId);
                
                TempData["SuccessMessage"] = "User deleted successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                var currentUserId = HttpContext.Session.GetInt32("UserID") ?? 0;
                _logger.LogError(ex, "Error during user deletion for ID: {TargetUserId} by User ID: {UserId}", 
                    id, currentUserId);
                
                // Show user-friendly error message from our improved service
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Index");
            }
        }
    }
}