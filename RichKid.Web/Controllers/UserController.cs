using Microsoft.AspNetCore.Mvc;
using RichKid.Web.Models;
using RichKid.Web.Services;
using RichKid.Web.Filters;

namespace RichKid.Web.Controllers
{
    public class UserController : Controller
    {
        private readonly UserService _userService = new();

        // רק קבוצה 1,2,3,4 (כל משתמש פעיל) מורשה לצפות
        [GroupAuthorize(1, 2, 3, 4)]
        public IActionResult Index(string search = "", string status = "")
        {
            var users = _userService.GetAllUsers();

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

        // רק מנהלים (1) ועורכים (2) מורשים ליצור
        [GroupAuthorize(1, 2)]
        public IActionResult Create() => View();

        [HttpPost, GroupAuthorize(1, 2)]
        public IActionResult Create(User user)
        {
            if (user.Data == null) user.Data = new UserData();

            if (ModelState.IsValid)
            {
                try
                {
                    _userService.AddUser(user);
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
        public IActionResult Edit(int id)
        {
            int group = HttpContext.Session.GetInt32("UserGroupID") ?? 0;
            int? me   = HttpContext.Session.GetInt32("UserID");

            // אם אני משתמש רגיל/צופה, מותר רק לערוך את עצמי
            if ((group == 3 || group == 4) && me != id)
                return Forbid();

            var user = _userService.GetUserById(id);
            return user == null ? NotFound() : View(user);
        }

        [HttpPost, GroupAuthorize(1, 2, 3, 4)]
        public IActionResult Edit(User user)
        {
            int group = HttpContext.Session.GetInt32("UserGroupID") ?? 0;
            int? me   = HttpContext.Session.GetInt32("UserID");

            if ((group == 3 || group == 4) && me != user.UserID)
                return Forbid();

            if (user.Data == null)
                user.Data = new UserData();

            if (ModelState.IsValid)
            {
                try
                {
                    _userService.UpdateUser(user);
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            return View(user);
        }

        // רק מנהלים (1) מורשים למחוק
        [GroupAuthorize(1)]
        public IActionResult Delete(int id)
        {
            var user = _userService.GetUserById(id);
            return user == null ? NotFound() : View(user);
        }

        [HttpPost, GroupAuthorize(1)]
        public IActionResult DeleteConfirmed(int id)
        {
            _userService.DeleteUser(id);
            return RedirectToAction("Index");
        }
    }
}
