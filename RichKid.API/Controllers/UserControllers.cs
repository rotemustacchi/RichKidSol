using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RichKid.Shared.Models;
using RichKid.Shared.Services;

namespace RichKid.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        // Constructor with dependency injection
        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        // GET /api/users
        [HttpGet]
        public ActionResult<List<User>> GetAll() =>
            _userService.GetAllUsers();

        // GET /api/users/{id}
        [HttpGet("{id}")]
        public ActionResult<User> GetById(int id)
        {
            var user = _userService.GetUserById(id);
            return user == null ? NotFound() : Ok(user);
        }

        // GET /api/users/search?firstName=X&lastName=Y
        [HttpGet("search")]
        public ActionResult<List<User>> Search(
            [FromQuery] string firstName,
            [FromQuery] string lastName)
        {
            var results = _userService.SearchByFullName(firstName, lastName);
            return Ok(results);
        }

        // POST /api/users
        [HttpPost]
        public IActionResult Create([FromBody] User user)
        {
            try
            {
                _userService.AddUser(user);
                return CreatedAtAction(nameof(GetById),
                    new { id = user.UserID }, user);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PUT /api/users/{id}
        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] User user)
        {
            // Validate that route ID matches user ID
            if (id != user.UserID)
                return BadRequest("Route ID does not match user ID");

            try
            {
                _userService.UpdateUser(user);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // DELETE /api/users/{id}
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            // Check if user exists before deletion
            var existing = _userService.GetUserById(id);
            if (existing == null)
                return NotFound();

            _userService.DeleteUser(id);
            return NoContent();
        }
    }
}