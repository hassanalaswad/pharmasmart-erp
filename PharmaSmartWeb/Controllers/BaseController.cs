using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PharmaSmartWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PharmaSmartWeb.Controllers
{
    public abstract class BaseController : Controller
    {
        protected readonly ApplicationDbContext _context;

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
            var controllerName = context.RouteData.Values["controller"]?.ToString() ?? "";
            var actionName = context.RouteData.Values["action"]?.ToString() ?? "";

            if (string.Equals(controllerName, "Account", StringComparison.OrdinalIgnoreCase)
                && (string.Equals(actionName, "Login", StringComparison.OrdinalIgnoreCase) || 
                    string.Equals(actionName, "Logout", StringComparison.OrdinalIgnoreCase)))
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

            PopulatePharmaSmartPageContext(controllerName, actionName);
        }

        /// <summary>
        /// يجهّز بيانات التخطيط والعناوين لكل طلب MVC مصادَق عليه (قائمة الشاشات، الفرع النشط، قسم النظام).
        /// </summary>
        private void PopulatePharmaSmartPageContext(string controllerName, string actionName)
        {
            ViewData["CurrentController"] = controllerName;
            ViewData["CurrentAction"] = actionName;
            ViewData["AppSection"] = ResolveAppSection(controllerName);

            try
            {
                var cache = HttpContext.RequestServices.GetService<IMemoryCache>();

                var allScreens = GetCachedSystemScreens(cache);
                bool HasPerm(string screenName) =>
                    IsSuperAdmin || User.HasClaim(c => c.Type == "Permission" && c.Value == $"{screenName}.View");

                foreach (var screen in allScreens)
                {
                    string cleanKey = screen.ScreenName.Replace(".", "");
                    ViewData[$"CanView{cleanKey}"] = HasPerm(screen.ScreenName);
                }

                ViewBag.SystemScreens = allScreens;

                bool showBranchSelector =
                    User.IsInRole("SuperAdmin") || User.HasClaim("Permission", "System.ChangeBranch");

                List<Branches>? activeBranches = null;
                if (IsSuperAdmin || showBranchSelector)
                    activeBranches = GetCachedActiveBranches(cache);

                if (IsSuperAdmin && activeBranches != null)
                {
                    if (Request.Cookies.TryGetValue("ActiveBranchId", out string? bId)
                        && int.TryParse(bId, out int cookieBranchId)
                        && cookieBranchId > 0
                        && !activeBranches.Any(b => b.BranchId == cookieBranchId))
                    {
                        Response.Cookies.Delete("ActiveBranchId");
                    }
                }

                if (showBranchSelector && activeBranches != null)
                    ViewBag.Branches = activeBranches;

                int scopeForUi = ReportScopeId;
                ViewBag.HeaderReportScopeId = scopeForUi;

                if (IsSuperAdmin && scopeForUi == 0)
                    ViewBag.ActiveBranchName = "المؤسسة (رؤية شاملة)";
                else if (activeBranches != null)
                {
                    var branch = activeBranches.FirstOrDefault(b => b.BranchId == ActiveBranchId);
                    ViewBag.ActiveBranchName = branch?.BranchName
                        ?? User.FindFirst("BranchName")?.Value
                        ?? "فرع غير محدد";
                }
                else
                    ViewBag.ActiveBranchName = User.FindFirst("BranchName")?.Value ?? "فرع غير محدد";
            }
            catch
            {
                ViewBag.SystemScreens = new List<Systemscreens>();
                ViewBag.ActiveBranchName = "—";
                ViewBag.Branches = null;
                ViewBag.HeaderReportScopeId = UserBranchId;
            }
        }

        private static string ResolveAppSection(string controllerName)
        {
            var c = controllerName?.ToUpperInvariant() ?? "";
            return c switch
            {
                "SALES" or "SALESRETURN" or "CUSTOMERS" => "التجاري",
                "PURCHASES" or "PURCHASESRETURN" or "SUPPLIERS" => "المشتريات",
                "DRUGS" or "INVENTORY" or "WAREHOUSES" or "ITEMGROUPS" or "DRUGTRANSFERS" or "STOCKAUDIT" or "INVENTORYINTELLIGENCE" => "المخزون والأصناف",
                "ACCOUNTING" or "JOURNALENTRIES" or "VOUCHERS" or "FINANCIALSETTINGS" or "FUNDTRANSFERS" or "CURRENCIES" => "المحاسبة والمالية",
                "REPORT" => "التقارير",
                "USERS" or "ROLES" or "EMPLOYEES" or "BRANCHES" or "ADMIN" => "الإدارة والصلاحيات",
                "HOME" => "لوحة التحكم",
                _ when c.Contains("REPORT", StringComparison.Ordinal) => "التقارير",
                _ => "Pharma Smart"
            };
        }

        private List<Systemscreens> GetCachedSystemScreens(IMemoryCache? cache)
        {
            if (cache == null)
                return _context.Systemscreens.AsNoTracking().ToList();

            return cache.GetOrCreate("Global_SystemScreens", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
                return _context.Systemscreens.AsNoTracking().ToList();
            })!;
        }

        private List<Branches> GetCachedActiveBranches(IMemoryCache? cache)
        {
            if (cache == null)
                return _context.Branches.AsNoTracking().Where(b => b.IsActive == true).ToList();

            return cache.GetOrCreate("Global_ActiveBranches", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
                return _context.Branches.AsNoTracking().Where(b => b.IsActive == true).ToList();
            })!;
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
