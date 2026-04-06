using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
// using PharmaSmartWeb.Filters; // ❌ Removed custom filter in favor of standardized Core Policies
using PharmaSmartWeb.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PharmaSmartWeb.Controllers
{
    // 🛡️ Policy Matrix Architecture: 
    // We enforce native Core Authorization Policies directly, ensuring centralized claim checks.
    [Authorize]
    public class RolesController : BaseController
    {
        private readonly IMemoryCache _cache;

        public RolesController(ApplicationDbContext context, IMemoryCache cache) : base(context)
        {
            _cache = cache;
        }

        // 1. عرض قائمة الأدوار (Index)
        [HttpGet]
        [Authorize(Policy = "Roles.View")] // 🚀 Centralized Policy mapping instead of scattered custom [HasPermission]
        public async Task<IActionResult> Index()
        {
            var query = _context.Userroles.AsQueryable();

            // 🚀 Architectural shift: Eliminating Magic Numbers (1) with SystemRoles.SuperAdmin
            if (!IsSuperAdmin)
            {
                query = query.Where(r => r.RoleId != (int)SystemRoles.SuperAdmin);
            }

            var roles = await query.OrderBy(r => r.RoleName).ToListAsync();

            return View(roles);
        }

        // 2. شاشة مصفوفة الصلاحيات (ManagePermissions)
        [HttpGet]
        [Authorize(Policy = "Roles.Edit")]
        public async Task<IActionResult> ManagePermissions(int roleId)
        {
            var role = await _context.Userroles.AsNoTracking().FirstOrDefaultAsync(r => r.RoleId == roleId);

            if (role == null) return NotFound();
            
            // 🚀 Secured URL tampering against non-SuperAdmins using exact Enum alignment
            if (!IsSuperAdmin && roleId == (int)SystemRoles.SuperAdmin) 
                return RedirectToAction("AccessDenied", "Home");

            ViewBag.RoleArabicName = role.RoleArabicName ?? role.RoleName;
            ViewBag.RoleId = roleId;

            var screens = await _context.Systemscreens
                .AsNoTracking()
                .OrderBy(s => s.ScreenCategory)
                .ThenBy(s => s.ScreenArabicName)
                .ToListAsync();

            var currentPermissions = await _context.Screenpermissions
                .Where(p => p.RoleId == roleId)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.CurrentPermissions = currentPermissions;

            return View(screens);
        }

        // 3. حفظ الصلاحيات (UpdatePermissions)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Roles.Edit")]
        public async Task<IActionResult> UpdatePermissions(int roleId, List<Screenpermissions> permissions)
        {
            if (!IsSuperAdmin && roleId == (int)SystemRoles.SuperAdmin) 
                return RedirectToAction("AccessDenied", "Home");

            var role = await _context.Userroles.FindAsync(roleId);
            if (role == null) return NotFound();

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var oldPermissions = _context.Screenpermissions.Where(p => p.RoleId == roleId);
                    _context.Screenpermissions.RemoveRange(oldPermissions);

                    if (permissions != null)
                    {
                        foreach (var perm in permissions)
                        {
                            perm.RoleId = roleId;
                            if (perm.CanView || perm.CanAdd || perm.CanEdit || perm.CanDelete)
                            {
                                _context.Screenpermissions.Add(perm);
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _cache.Remove($"Permissions_Role_{roleId}");
                    await RecordLog("Update", "Permissions", $"تحديث صلاحيات: ({role.RoleArabicName ?? role.RoleName})");

                    TempData["Success"] = "تم حفظ مصفوفة الصلاحيات بنجاح.";
                }
                catch
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = "حدث خطأ أثناء حفظ البيانات.";
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
