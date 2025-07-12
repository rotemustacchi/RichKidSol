using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace RichKid.Web.Filters
{
    public class JwtAuthorizeAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        private readonly string[] _requiredPermissions;
        private readonly bool _allowSelfEdit;

        public JwtAuthorizeAttribute(params string[] requiredPermissions)
        {
            _requiredPermissions = requiredPermissions;
            _allowSelfEdit = false;
        }

        public JwtAuthorizeAttribute(bool allowSelfEdit, params string[] requiredPermissions)
        {
            _requiredPermissions = requiredPermissions;
            _allowSelfEdit = allowSelfEdit;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            // Check if user is authenticated
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            // Get user's permissions from JWT claims
            var userGroupId = user.FindFirst("UserGroupID")?.Value;
            
            if (string.IsNullOrEmpty(userGroupId) || userGroupId == "0")
            {
                // Store error message in session for display
                context.HttpContext.Session.SetString("AuthError", "You don't have a user group assigned. Please contact an administrator.");
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            // Check specific permissions
            bool hasPermission = false;

            foreach (var permission in _requiredPermissions)
            {
                var claim = user.FindFirst(permission)?.Value;
                if (claim == "true")
                {
                    hasPermission = true;
                    break;
                }
                else if (claim == "self" && _allowSelfEdit)
                {
                    // Check if user is trying to edit themselves
                    var routeUserId = context.RouteData.Values["id"]?.ToString();
                    var currentUserId = user.FindFirst("UserID")?.Value;
                    
                    if (routeUserId == currentUserId)
                    {
                        hasPermission = true;
                        break;
                    }
                }
            }

            if (!hasPermission)
            {
                // Store error message in session for display
                context.HttpContext.Session.SetString("AuthError", "You don't have permission to perform this action.");
                context.Result = new ForbidResult();
                return;
            }
        }
    }

    // Convenience attributes for common scenarios
    public class RequireViewPermissionAttribute : JwtAuthorizeAttribute
    {
        public RequireViewPermissionAttribute() : base("CanView") { }
    }

    public class RequireCreatePermissionAttribute : JwtAuthorizeAttribute
    {
        public RequireCreatePermissionAttribute() : base("CanCreate") { }
    }

    public class RequireEditPermissionAttribute : JwtAuthorizeAttribute
    {
        public RequireEditPermissionAttribute(bool allowSelfEdit = false) : base(allowSelfEdit, "CanEdit") { }
    }

    public class RequireDeletePermissionAttribute : JwtAuthorizeAttribute
    {
        public RequireDeletePermissionAttribute() : base("CanDelete") { }
    }
}