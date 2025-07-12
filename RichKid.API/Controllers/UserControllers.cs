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

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        // GET /api/users - All authenticated users can view
        [HttpGet]
        [Authorize(Policy = "CanView")]
        public ActionResult<List<User>> GetAll() =>
            _userService.GetAllUsers();

        // GET /api/users/{id}
        [HttpGet("{id}")]
        [Authorize(Policy = "CanView")]
        public ActionResult<User> GetById(int id)
        {
            var user = _userService.GetUserById(id);
            return user == null ? NotFound("User not found") : Ok(user);
        }

        // GET /api/users/search?firstName=X&lastName=Y
        [HttpGet("search")]
        [Authorize(Policy = "CanView")]
        public ActionResult<List<User>> Search(
            [FromQuery] string firstName,
            [FromQuery] string lastName)
        {
            var results = _userService.SearchByFullName(firstName, lastName);
            return Ok(results);
        }

        // POST /api/users - Only admins and editors can create
        [HttpPost]
        [Authorize(Policy = "CanCreate")]
        public IActionResult Create([FromBody] User user)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .SelectMany(x => x.Value.Errors)
                    .Select(x => x.ErrorMessage)
                    .ToList();
                
                return BadRequest(string.Join(". ", errors));
            }

            try
            {
                _userService.AddUser(user);
                return CreatedAtAction(nameof(GetById),
                    new { id = user.UserID }, user);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Username already exists"))
                {
                    return BadRequest("Username already exists in the system");
                }
                return BadRequest($"Error creating user: {ex.Message}");
            }
        }

        // PUT /api/users/{id} - Admins/editors can edit anyone, users can edit themselves
        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] User user)
        {
            if (id != user.UserID)
                return BadRequest("User ID mismatch");

            // Check permissions
            var currentUserId = User.FindFirst("UserID")?.Value;
            var canEdit = User.HasClaim("CanEdit", "true");
            var canEditSelf = User.HasClaim("CanEdit", "self") && currentUserId == id.ToString();

            if (!canEdit && !canEditSelf)
            {
                return Forbid("You don't have permission to edit this user");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .SelectMany(x => x.Value.Errors)
                    .Select(x => x.ErrorMessage)
                    .ToList();
                
                return BadRequest(string.Join(". ", errors));
            }

            try
            {
                _userService.UpdateUser(user);
                return NoContent();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Username already exists"))
                {
                    return BadRequest("Username already exists in the system");
                }
                return BadRequest($"Error updating user: {ex.Message}");
            }
        }

        // DELETE /api/users/{id} - Only admins can delete
        [HttpDelete("{id}")]
        [Authorize(Policy = "CanDelete")]
        public IActionResult Delete(int id)
        {
            var existing = _userService.GetUserById(id);
            if (existing == null)
                return NotFound("User not found");

            try
            {
                _userService.DeleteUser(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest($"Error deleting user: {ex.Message}");
            }
        }
    }
}