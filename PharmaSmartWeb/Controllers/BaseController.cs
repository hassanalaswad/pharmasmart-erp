
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PharmaSmartWeb.Controllers
{
    // 🚀 التصحيح الأهم: جعل الكلاس abstract لمنع النظام من محاولة تشغيله ككنترولر مستقل
    public abstract class BaseController : Controller
    {
        protected readonly ApplicationDbContext _context;

        // 🚀 يجب أن يكون هناك مُنشئ (Constructor) واحد فقط كما يلي
        public BaseController(ApplicationDbContext context)
        {
            _context = context;
        }

        protected int UserBranchId => int.Parse(User.FindFirst("BranchID")?.Value ?? "1");
        protected int UserRoleId => int.Parse(User.FindFirst("RoleID")?.Value ?? "0");
        protected bool IsSuperAdmin => UserRoleId == 1;

        public int ActiveBranchId => (IsSuperAdmin && Request.Cookies.TryGetValue("ActiveBranchId", out string? bId) && int.TryParse(bId, out int id) && id > 0) ? id : UserBranchId;
        public int ReportScopeId => (IsSuperAdmin && Request.Cookies.TryGetValue("ActiveBranchId", out string? bId) && int.TryParse(bId, out int id) && id >= 0) ? id : UserBranchId;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // جلب معلومات المسار الحالي
            var controllerName = context.RouteData.Values["controller"]?.ToString();
            var actionName = context.RouteData.Values["action"]?.ToString();

            // 🛑 السماح بفتح صفحة تسجيل الدخول دون فحص الهوية لمنع الـ 404 والـ Loop
            if (controllerName == "Account" && actionName == "Login")
            {
                base.OnActionExecuting(context);
                return;
            }

            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl = Request.Path });
                return;
            }

            base.OnActionExecuting(context);

            if (User.Identity.IsAuthenticated)
            {
                // ✅ الإصلاح: تغليف استعلامات DB في try-catch لمنع انهيار كل الطلبات عند فشل الاتصال
                try
                {
                    var allScreens = _context.Systemscreens.AsNoTracking().ToList();
                    bool HasPerm(string screenName) => IsSuperAdmin || User.HasClaim(c => c.Type == "Permission" && c.Value == $"{screenName}.View");

                    foreach (var screen in allScreens)
                    {
                        string cleanKey = screen.ScreenName.Replace(".", "");
                        ViewData[$"CanView{cleanKey}"] = HasPerm(screen.ScreenName);
                    }

                    ViewBag.SystemScreens = allScreens;

                    if (IsSuperAdmin)
                    {
                        // ✅ التحقق من صحة ActiveBranchId: يجب أن يكون فرعاً موجوداً فعلاً في النظام
                        var allBranches = _context.Branches.AsNoTracking().Where(b => b.IsActive == true).ToList();
                        ViewBag.Branches = allBranches;

                        // إذا كان كوكيز الفرع يشير لفرع غير موجود — يتم تجاهله وإعادة الفرع الافتراضي
                        if (Request.Cookies.TryGetValue("ActiveBranchId", out string? bId)
                            && int.TryParse(bId, out int cookieBranchId)
                            && cookieBranchId > 0
                            && !allBranches.Any(b => b.BranchId == cookieBranchId))
                        {
                            Response.Cookies.Delete("ActiveBranchId");
                        }
                    }

                    if (IsSuperAdmin && ReportScopeId == 0)
                        ViewBag.ActiveBranchName = "المؤسسة (رؤية شاملة)";
                    else
                    {
                        var branch = _context.Branches.AsNoTracking().FirstOrDefault(b => b.BranchId == ActiveBranchId);
                        ViewBag.ActiveBranchName = branch?.BranchName ?? "فرع غير محدد";
                    }
                }
                catch
                {
                    // الفشل الصامت عند استعلامات التنقل لا يجب أن يوقف الطلب
                    ViewBag.SystemScreens = new List<PharmaSmartWeb.Models.Systemscreens>();
                    ViewBag.ActiveBranchName = "—";
                }
            }
        }

        protected async Task RecordLog(string action, string screen, string details)
        {
            try
            {
                var log = new SystemLogs
                {
                    UserId = int.Parse(User.FindFirst("UserID")?.Value ?? "0"),
                    Action = action,
                    ScreenName = screen,
                    Details = $"[فرع {ActiveBranchId}] - {details}",
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    CreatedAt = DateTime.Now
                };
                _context.Systemlogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch { }
        }
        public override RedirectResult Redirect(string url)
        {
            return base.Redirect(url);
        }


    }
}