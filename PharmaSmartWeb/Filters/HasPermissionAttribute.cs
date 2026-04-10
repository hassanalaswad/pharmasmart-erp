//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Filters;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Caching.Memory;
//using PharmaSmartWeb.Models;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace PharmaSmartWeb.Filters
//{
//    public class HasPermissionAttribute : TypeFilterAttribute
//    {
//        public HasPermissionAttribute(string screenName, string action)
//            : base(typeof(PermissionFilter))
//        {
//            Arguments = new object[] { screenName, action };
//        }
//    }

//    public class PermissionFilter : IAsyncAuthorizationFilter
//    {
//        private readonly string _screenName;
//        private readonly string _action;
//        private readonly ApplicationDbContext _db;
//        private readonly IMemoryCache _cache;

//        public PermissionFilter(string screenName, string action, ApplicationDbContext db, IMemoryCache cache)
//        {
//            _screenName = screenName;
//            _action = action;
//            _db = db;
//            _cache = cache;
//        }

//        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
//        {
//            var user = context.HttpContext.User;

//            if (!user.Identity.IsAuthenticated)
//            {
//                context.Result = new RedirectToActionResult("Login", "Account", null);
//                return;
//            }

//            var roleIdStr = user.FindFirst("RoleID")?.Value ?? user.FindFirst("RoleId")?.Value;
//            if (string.IsNullOrEmpty(roleIdStr) || !int.TryParse(roleIdStr, out int roleId))
//            {
//                context.Result = new RedirectToActionResult("AccessDenied", "Home", null);
//                return;
//            }

//            string cacheKey = $"Permissions_Role_{roleId}";

//            if (!_cache.TryGetValue(cacheKey, out List<Screenpermissions> rolePermissions))
//            {
//                try
//                {
//                    // 🚀 الحل الجذري لمنع الخطأ الفني (500)
//                    var rawPermissions = await _db.Screenpermissions
//                        .Include(p => p.Screen)
//                        .Where(p => p.RoleId == roleId)
//                        .ToListAsync();

//                    rolePermissions = rawPermissions.Where(p => p.Screen != null).ToList();

//                    var cacheOptions = new MemoryCacheEntryOptions()
//                        .SetSlidingExpiration(TimeSpan.FromHours(12));

//                    _cache.Set(cacheKey, rolePermissions, cacheOptions);
//                }
//                catch (Exception)
//                {
//                    // الحماية القصوى: توجيه المستخدم لصفحة منع الوصول بدلاً من الشاشة البيضاء المخيفة
//                    context.Result = new RedirectToActionResult("AccessDenied", "Home", null);
//                    return;
//                }
//            }

//            bool hasAccess = false;
//            var targetPermission = rolePermissions?.FirstOrDefault(p => p.Screen.ScreenName == _screenName);

//            if (targetPermission != null)
//            {
//                hasAccess = _action switch
//                {
//                    "View" => targetPermission.CanView,
//                    "Add" => targetPermission.CanAdd,
//                    "Edit" => targetPermission.CanEdit,
//                    "Delete" => targetPermission.CanDelete,
//                    "Print" => targetPermission.CanPrint,
//                    _ => false
//                };
//            }

//            if (!hasAccess)
//            {
//                context.Result = new RedirectToActionResult("AccessDenied", "Home", null);
//            }
//        }
//    }
//}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PharmaSmartWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PharmaSmartWeb.Filters
{
    public class HasPermissionAttribute : TypeFilterAttribute
    {
        public HasPermissionAttribute(string screenName, string action)
            : base(typeof(PermissionFilter))
        {
            Arguments = new object[] { screenName, action };
        }
    }

    public class PermissionFilter : IAsyncAuthorizationFilter
    {
        private readonly string _screenName;
        private readonly string _action;
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;

        public PermissionFilter(string screenName, string action, ApplicationDbContext db, IMemoryCache cache)
        {
            _screenName = screenName;
            _action = action;
            _db = db;
            _cache = cache;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (!user.Identity.IsAuthenticated)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            var roleIdStr = user.FindFirst("RoleID")?.Value ?? user.FindFirst("RoleId")?.Value;
            if (string.IsNullOrEmpty(roleIdStr) || !int.TryParse(roleIdStr, out int roleId))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Home", null);
                return;
            }

            if (roleId == 1)
            {
                return; // الخروج من الفلتر والسماح بالوصول
            }

            string cacheKey = $"Permissions_Role_{roleId}";

            if (!_cache.TryGetValue(cacheKey, out List<Screenpermissions> rolePermissions))
            {
                try
                {
                    // 🚀 الحل الجذري لمنع الخطأ الفني (500)
                    var rawPermissions = await _db.Screenpermissions
                        .Include(p => p.Screen)
                        .Where(p => p.RoleId == roleId)
                        .ToListAsync();

                    rolePermissions = rawPermissions.Where(p => p.Screen != null).ToList();

                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromMinutes(30));

                    _cache.Set(cacheKey, rolePermissions, cacheOptions);
                }
                catch (Exception)
                {
                    // الحماية القصوى: توجيه المستخدم لصفحة منع الوصول بدلاً من الشاشة البيضاء المخيفة
                    context.Result = new RedirectToActionResult("AccessDenied", "Home", null);
                    return;
                }
            }

            bool hasAccess = false;
            var targetPermission = rolePermissions?.FirstOrDefault(p => p.Screen.ScreenName == _screenName);

            if (targetPermission != null)
            {
                hasAccess = _action switch
                {
                    "View" => targetPermission.CanView,
                    "Add" => targetPermission.CanAdd,
                    "Edit" => targetPermission.CanEdit,
                    "Delete" => targetPermission.CanDelete,
                    "Print" => targetPermission.CanPrint,
                    _ => false
                };
            }

            if (!hasAccess)
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Home", null);
            }
        }
    }
}