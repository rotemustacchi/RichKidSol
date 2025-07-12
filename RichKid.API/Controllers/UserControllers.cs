using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RichKid.Shared.Models;
using RichKid.Shared.Services;
using System.Security.Claims;

namespace RichKid.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger; // Add logger for tracking user operations

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger; // Inject logger to monitor all user management activities
        }

        // GET /api/users - All authenticated users can view
        [HttpGet]
        [Authorize(Policy = "CanView")]
        public ActionResult<List<User>> GetAll()
        {
            try
            {
                // Get current user information from JWT claims for logging
                var currentUserId = User.FindFirst("UserID")?.Value ?? "Unknown";
                var currentUserName = User.Identity?.Name ?? "Unknown";
                
                _logger.LogInformation("GetAll users request started by user: {UserName} (ID: {UserId})", 
                    currentUserName, currentUserId);
                
                // Retrieve all users from the service
                var users = _userService.GetAllUsers();
                
                _logger.LogInformation("GetAll users completed successfully. Retrieved {UserCount} users for user: {UserName}", 
                    users.Count, currentUserName);
                
                return users;
            }
            catch (Exception ex)
            {
                // Log any errors that occur during user retrieval
                _logger.LogError(ex, "Error occurred while retrieving all users");
                return StatusCode(500, "An error occurred while retrieving users");
            }
        }

        // GET /api/users/{id}
        [HttpGet("{id}")]
        [Authorize(Policy = "CanView")]
        public ActionResult<User> GetById(int id)
        {
            try
            {
                // Get current user information for logging
                var currentUserId = User.FindFirst("UserID")?.Value ?? "Unknown";
                var currentUserName = User.Identity?.Name ?? "Unknown";
                
                _logger.LogInformation("GetById request for user ID: {RequestedUserId} by user: {UserName} (ID: {UserId})", 
                    id, currentUserName, currentUserId);
                
                // Attempt to find the requested user
                var user = _userService.GetUserById(id);
                
                if (user == null)
                {
                    _logger.LogWarning("User not found with ID: {RequestedUserId}, requested by: {UserName}", 
                        id, currentUserName);
                    return NotFound("User not found");
                }
                
                _logger.LogInformation("GetById completed successfully. User {RequestedUserName} (ID: {RequestedUserId}) retrieved by: {UserName}", 
                    user.UserName, id, currentUserName);
                
                return Ok(user);
            }
            catch (Exception ex)
            {
                // Log errors during user lookup
                _logger.LogError(ex, "Error occurred while retrieving user with ID: {RequestedUserId}", id);
                return StatusCode(500, "An error occurred while retrieving the user");
            }
        }

        // GET /api/users/search?firstName=X&lastName=Y
        [HttpGet("search")]
        [Authorize(Policy = "CanView")]
        public ActionResult<List<User>> Search(
            [FromQuery] string firstName,
            [FromQuery] string lastName)
        {
            try
            {
                // Get current user information for logging
                var currentUserId = User.FindFirst("UserID")?.Value ?? "Unknown";
                var currentUserName = User.Identity?.Name ?? "Unknown";
                
                _logger.LogInformation("Search users request by user: {UserName} (ID: {UserId}) - FirstName: '{FirstName}', LastName: '{LastName}'", 
                    currentUserName, currentUserId, firstName ?? "null", lastName ?? "null");
                
                // Perform the search using the service
                var results = _userService.SearchByFullName(firstName, lastName);
                
                _logger.LogInformation("Search completed successfully. Found {ResultCount} users matching criteria for user: {UserName}", 
                    results.Count, currentUserName);
                
                return Ok(results);
            }
            catch (Exception ex)
            {
                // Log search errors
                _logger.LogError(ex, "Error occurred during user search with FirstName: '{FirstName}', LastName: '{LastName}'", 
                    firstName, lastName);
                return StatusCode(500, "An error occurred during the search");
            }
        }

        // POST /api/users - Only admins and editors can create
        [HttpPost]
        [Authorize(Policy = "CanCreate")]
        public IActionResult Create([FromBody] User user)
        {
            try
            {
                // Get current user information for logging
                var currentUserId = User.FindFirst("UserID")?.Value ?? "Unknown";
                var currentUserName = User.Identity?.Name ?? "Unknown";
                
                _logger.LogInformation("Create user request started by user: {UserName} (ID: {UserId}) for new user: {NewUserName}", 
                    currentUserName, currentUserId, user.UserName);
                
                // Validate the model state first
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value.Errors)
                        .Select(x => x.ErrorMessage)
                        .ToList();
                    
                    _logger.LogWarning("Create user validation failed for user: {NewUserName}. Errors: {ValidationErrors}", 
                        user.UserName, string.Join(", ", errors));
                    
                    return BadRequest(string.Join(". ", errors));
                }

                // Attempt to create the new user
                _userService.AddUser(user);
                
                _logger.LogInformation("User created successfully: {NewUserName} (ID: {NewUserId}) by user: {UserName}", 
                    user.UserName, user.UserID, currentUserName);
                
                return CreatedAtAction(nameof(GetById),
                    new { id = user.UserID }, user);
            }
            catch (Exception ex)
            {
                // Handle specific username conflict errors
                if (ex.Message.Contains("Username already exists"))
                {
                    _logger.LogWarning("Create user failed - Username already exists: {NewUserName}, requested by: {UserName}", 
                        user.UserName, User.Identity?.Name ?? "Unknown");
                    return BadRequest("Username already exists in the system");
                }
                
                // Log other creation errors
                _logger.LogError(ex, "Error occurred while creating user: {NewUserName}", user.UserName);
                return BadRequest($"Error creating user: {ex.Message}");
            }
        }

        // PUT /api/users/{id} - Admins/editors can edit anyone, users can edit themselves
        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] User user)
        {
            try
            {
                // Get current user information for logging
                var currentUserId = User.FindFirst("UserID")?.Value ?? "Unknown";
                var currentUserName = User.Identity?.Name ?? "Unknown";
                
                _logger.LogInformation("Update user request started by user: {UserName} (ID: {UserId}) for user ID: {TargetUserId}", 
                    currentUserName, currentUserId, id);
                
                // Ensure the ID in the URL matches the user object
                if (id != user.UserID)
                {
                    _logger.LogWarning("Update user failed - ID mismatch. URL ID: {UrlId}, User ID: {UserId}, requested by: {UserName}", 
                        id, user.UserID, currentUserName);
                    return BadRequest("User ID mismatch");
                }

                // Check if the current user has permission to edit this user
                var canEdit = User.HasClaim("CanEdit", "true");
                var canEditSelf = User.HasClaim("CanEdit", "self") && currentUserId == id.ToString();

                if (!canEdit && !canEditSelf)
                {
                    _logger.LogWarning("Update user failed - Insufficient permissions. User: {UserName} attempted to edit user ID: {TargetUserId}", 
                        currentUserName, id);
                    return Forbid("You don't have permission to edit this user");
                }

                _logger.LogDebug("Permission check passed for user: {UserName} editing user ID: {TargetUserId}", 
                    currentUserName, id);

                // Validate the model state
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value.Errors)
                        .Select(x => x.ErrorMessage)
                        .ToList();
                    
                    _logger.LogWarning("Update user validation failed for user ID: {TargetUserId}. Errors: {ValidationErrors}", 
                        id, string.Join(", ", errors));
                    
                    return BadRequest(string.Join(". ", errors));
                }

                // Attempt to update the user
                _userService.UpdateUser(user);
                
                _logger.LogInformation("User updated successfully: {UpdatedUserName} (ID: {TargetUserId}) by user: {UserName}", 
                    user.UserName, id, currentUserName);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                // Handle specific username conflict errors
                if (ex.Message.Contains("Username already exists"))
                {
                    _logger.LogWarning("Update user failed - Username already exists: {UserName} for user ID: {TargetUserId}", 
                        user.UserName, id);
                    return BadRequest("Username already exists in the system");
                }
                
                // Log other update errors
                _logger.LogError(ex, "Error occurred while updating user ID: {TargetUserId}", id);
                return BadRequest($"Error updating user: {ex.Message}");
            }
        }

        // DELETE /api/users/{id} - Only admins can delete
        [HttpDelete("{id}")]
        [Authorize(Policy = "CanDelete")]
        public IActionResult Delete(int id)
        {
            try
            {
                // Get current user information for logging
                var currentUserId = User.FindFirst("UserID")?.Value ?? "Unknown";
                var currentUserName = User.Identity?.Name ?? "Unknown";
                
                _logger.LogInformation("Delete user request started by user: {UserName} (ID: {UserId}) for user ID: {TargetUserId}", 
                    currentUserName, currentUserId, id);
                
                // Check if the user exists before attempting deletion
                var existing = _userService.GetUserById(id);
                if (existing == null)
                {
                    _logger.LogWarning("Delete user failed - User not found with ID: {TargetUserId}, requested by: {UserName}", 
                        id, currentUserName);
                    return NotFound("User not found");
                }

                var userNameToDelete = existing.UserName;
                
                // Attempt to delete the user
                _userService.DeleteUser(id);
                
                _logger.LogInformation("User deleted successfully: {DeletedUserName} (ID: {TargetUserId}) by user: {UserName}", 
                    userNameToDelete, id, currentUserName);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                // Log deletion errors
                _logger.LogError(ex, "Error occurred while deleting user ID: {TargetUserId}", id);
                return BadRequest($"Error deleting user: {ex.Message}");
            }
        }
    }
}