using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RichKid.Web.Filters
{
    public class GroupAuthorizeAttribute : ActionFilterAttribute
    {
        private readonly int[] _allowedGroups;
        public GroupAuthorizeAttribute(params int[] allowedGroups)
        {
            _allowedGroups = allowedGroups;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            var groupId = session.GetInt32("UserGroupID") ?? 0;

            if (!_allowedGroups.Contains(groupId))
            {
                // מנקה סשן, אם תרצה:
                // context.HttpContext.Session.Clear();

                // מגדירים הודעה שתוצג ב-Login
                if (context.Controller is Controller c)
                {
                    c.TempData["AuthError"] = "אין לך קבוצת שייכות, אנא פנה למנהל";
                }

                // מפנים למסך ה-Login
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
