using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PharmaSmartWeb.Infrastructure
{
    /// <summary>
    /// يُزرع صلاحيات الأدوار الافتراضية تلقائياً عند أول تشغيل للنظام.
    /// يعمل فقط إذا كانت صلاحيات مدير الفرع (RoleId=2) فارغة.
    /// </summary>
    public static class PermissionSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext db)
        {
            // إذا كانت صلاحيات RoleId=2 موجودة → لا نفعل شيئاً
            bool alreadySeeded = await db.Screenpermissions.AnyAsync(p => p.RoleId == 2);
            if (alreadySeeded) return;

            var screens = await db.Systemscreens.AsNoTracking().ToListAsync();
            if (!screens.Any()) return;

            // الشاشات التي لا يجوز لمدير الفرع تعديلها (عرض فقط)
            var adminOnlyScreens = new HashSet<string> { "Users", "Roles", "Branches" };
            // الشاشات التي لا يجوز لمدير الفرع حذف منها
            var noDeleteScreens = new HashSet<string> { "Users", "Roles", "Branches", "JournalEntries" };

            var branchManagerPerms = new List<Screenpermissions>();
            var pharmacistPerms    = new List<Screenpermissions>();

            // شاشات الصيدلاني المسموح بها
            var pharmacistViewScreens = new HashSet<string>
            {
                "Sales", "SalesReturn", "Drugs", "Inventory", "Customers",
                "BarcodeGenerator", "ShortageForecast", "Suppliers",
                "Purchases", "PurchasesReturn"
            };
            var pharmacistAddScreens = new HashSet<string> { "Sales", "SalesReturn" };
            var pharmacistPrintScreens = new HashSet<string> { "Sales", "BarcodeGenerator" };

            foreach (var screen in screens)
            {
                // ── مدير الفرع (RoleId=2) ──────────────────────────────────
                branchManagerPerms.Add(new Screenpermissions
                {
                    RoleId    = 2,
                    ScreenId  = screen.ScreenId,
                    CanView   = true,
                    CanAdd    = !adminOnlyScreens.Contains(screen.ScreenName),
                    CanEdit   = !adminOnlyScreens.Contains(screen.ScreenName),
                    CanDelete = !noDeleteScreens.Contains(screen.ScreenName),
                    CanPrint  = true
                });

                // ── الصيدلاني/الكاشير (RoleId=3) ─────────────────────────
                pharmacistPerms.Add(new Screenpermissions
                {
                    RoleId    = 3,
                    ScreenId  = screen.ScreenId,
                    CanView   = pharmacistViewScreens.Contains(screen.ScreenName),
                    CanAdd    = pharmacistAddScreens.Contains(screen.ScreenName),
                    CanEdit   = false,
                    CanDelete = false,
                    CanPrint  = pharmacistPrintScreens.Contains(screen.ScreenName)
                });
            }

            db.Screenpermissions.AddRange(branchManagerPerms);
            db.Screenpermissions.AddRange(pharmacistPerms);
            await db.SaveChangesAsync();
        }
    }
}
