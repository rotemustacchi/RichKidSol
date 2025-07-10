using Microsoft.AspNetCore.Mvc;
using RichKid.Web.Models;
using RichKid.Web.Services;

namespace RichKid.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly UserService _userService = new();

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            // 1️⃣ מציאת משתמש ע"י שם וסיסמה (בלי להתחשב ב-Active)
            var user = _userService
                .GetAllUsers()
                .FirstOrDefault(u => u.UserName == username && u.Password == password);

            if (user == null)
            {
                // לא נמצאה התאמה בכלל
                ViewBag.Error = "שם משתמש או סיסמה שגויים.";
                return View();
            }

            if (!user.Active)
            {
                // משתמש קיים אך מסומן כלא פעיל
                ViewBag.Error = "המשתמש לא פעיל, אנא פנה למנהל.";
                return View();
            }

            // 2️⃣ משתמש תקין + פעיל — ניצור לו סשן ונמשיך
            HttpContext.Session.SetInt32("UserID", user.UserID);
            HttpContext.Session.SetInt32("UserGroupID", user.UserGroupID ?? 0);
            return RedirectToAction("Index", "User");
        }


        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
