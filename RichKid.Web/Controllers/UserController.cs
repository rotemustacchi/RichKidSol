using Microsoft.AspNetCore.Mvc;
using RichKid.Web.Models;
using RichKid.Web.Services;
using RichKid.Web.Filters;

namespace RichKid.Web.Controllers
{
    public class UserController : Controller
    {
        private readonly IUserService _userService;

        // Use dependency injection instead of manual instantiation
        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // Only groups 1,2,3,4 (all active users) are authorized to view
        [GroupAuthorize(1, 2, 3, 4)]
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

        // Only admins (1) and editors (2) are authorized to create
        [GroupAuthorize(1, 2)]
        public IActionResult Create() => View();

        [HttpPost, GroupAuthorize(1, 2)]
        public async Task<IActionResult> Create(User user)
        {
            if (user.Data == null) user.Data = new UserData();

            if (ModelState.IsValid)
            {
                try
                {
                    await _userService.AddUserAsync(user);
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            return View(user);
        }

        [GroupAuthorize(1, 2, 3, 4)]
        public async Task<IActionResult> Edit(int id)
        {
            int group = HttpContext.Session.GetInt32("UserGroupID") ?? 0;
            int? me = HttpContext.Session.GetInt32("UserID");

            // If I'm a regular user/viewer, only allowed to edit myself
            if ((group == 3 || group == 4) && me != id)
                return Forbid();

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

        [HttpPost, GroupAuthorize(1, 2, 3, 4)]
        public async Task<IActionResult> Edit(User user)
        {
            int group = HttpContext.Session.GetInt32("UserGroupID") ?? 0;
            int? me = HttpContext.Session.GetInt32("UserID");

            if ((group == 3 || group == 4) && me != user.UserID)
                return Forbid();

            if (user.Data == null)
                user.Data = new UserData();

            if (ModelState.IsValid)
            {
                try
                {
                    await _userService.UpdateUserAsync(user);
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            return View(user);
        }

        // Only admins (1) are authorized to delete
        [GroupAuthorize(1)]
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

        [HttpPost, GroupAuthorize(1)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _userService.DeleteUserAsync(id);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error deleting user: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
    }
}