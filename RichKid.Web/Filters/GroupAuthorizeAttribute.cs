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
                // Optionally clear the session (e.g. to fully reset user context)
                // context.HttpContext.Session.Clear();

                // Set a TempData error message to be displayed on the login page
                if (context.Controller is Controller c)
                {
                    c.TempData["AuthError"] = "You do not have the required group permissions. Please contact the administrator.";
                }

                // Redirect the user to the Login page
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
