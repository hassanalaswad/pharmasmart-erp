using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PharmaSmartWeb.Services
{
    /// <summary>
    /// محرك الإشعارات التلقائي — يفحص قاعدة البيانات وينشئ الإشعارات.
    /// يتم استدعاؤه من أي كنترولر أو من خلال Hosted Service دوري.
    /// </summary>
    public class NotificationEngine
    {
        private readonly ApplicationDbContext _context;

        public NotificationEngine(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// الدالة الرئيسية: تفحص قاعدة البيانات وتولّد جميع الإشعارات الجديدة.
        /// </summary>
        public async Task GenerateAndSaveNotificationsAsync(int branchId = 0)
        {
            var today = DateTime.Today;
            var newNotifications = new List<SystemNotification>();

            // ── 1. أدوية انتهت صلاحيتها ──────────────────────────────
            var expired = await _context.Purchasedetails
                .Include(pd => pd.Drug)
                .Where(pd => pd.ExpiryDate < today && pd.RemainingQuantity > 0)
                .ToListAsync();

            foreach (var b in expired)
            {
                bool exists = await _context.SystemNotifications
                    .AnyAsync(n => n.Title == $"صلاحية منتهية: {b.Drug.DrugName} (باتش: {b.BatchNumber})" && n.IsRead == false);
                if (!exists)
                    newNotifications.Add(new SystemNotification
                    {
                        Category   = "expiry",
                        Severity   = "critical",
                        Icon       = "dangerous",
                        IconColor  = "text-red-600",
                        BgColor    = "bg-red-50 border-red-200",
                        BadgeColor = "bg-red-600",
                        Title      = $"صلاحية منتهية: {b.Drug?.DrugName} (باتش: {b.BatchNumber})",
                        Body       = $"الباتش {b.BatchNumber} — انتهت {b.ExpiryDate:dd/MM/yyyy} (الكمية المتبقية: {b.RemainingQuantity})",
                        ActionUrl  = "/Report/StockExpiry",
                        ActionText = "عرض تقرير الصلاحية",
                        BranchId   = branchId
                    });
            }

            // ── 2. أدوية توشك على الانتهاء ─────────────────────
            var expiring = await _context.Purchasedetails
                .Include(pd => pd.Drug)
                .Where(pd => pd.ExpiryDate >= today && pd.ExpiryDate <= today.AddMonths(3) && pd.RemainingQuantity > 0)
                .ToListAsync();

            foreach (var b in expiring)
            {
                int days = (b.ExpiryDate - today).Days;
                bool exists = await _context.SystemNotifications
                    .AnyAsync(n => n.Title == $"صلاحية مقاربة: {b.Drug.DrugName} (باتش: {b.BatchNumber})" && n.IsRead == false);
                if (!exists)
                    newNotifications.Add(new SystemNotification
                    {
                        Category   = "expiry",
                        Severity   = "warning",
                        Icon       = "event_busy",
                        IconColor  = "text-amber-600",
                        BgColor    = "bg-amber-50 border-amber-200",
                        BadgeColor = "bg-amber-500",
                        Title      = $"صلاحية مقاربة: {b.Drug?.DrugName} (باتش: {b.BatchNumber})",
                        Body       = $"الباتش {b.BatchNumber} — تنتهي بعد {days} يوم ({b.ExpiryDate:dd/MM/yyyy})",
                        ActionUrl  = "/Report/StockExpiry",
                        ActionText = "إدارة الصلاحية",
                        BranchId   = branchId
                    });
            }

            // ── 3. نواقص المخزون ──────────────────────────────────────────────
            var invQ = _context.Branchinventory.Include(bi => bi.Drug).AsQueryable();
            if (branchId > 0) invQ = invQ.Where(bi => bi.BranchId == branchId);

            var shortages = await invQ
                .Where(bi => bi.StockQuantity <= bi.MinimumStockLevel)
                .ToListAsync();

            foreach (var s in shortages)
            {
                bool empty = s.StockQuantity <= 0;
                string title = empty ? $"نفد من المخزون: {s.Drug?.DrugName}" : $"مخزون حرج: {s.Drug?.DrugName}";
                
                bool exists = await _context.SystemNotifications
                    .AnyAsync(n => n.Title == title && n.IsRead == false);
                if (!exists)
                    newNotifications.Add(new SystemNotification
                    {
                        Category   = "shortage",
                        Severity   = empty ? "critical" : "warning",
                        Icon       = empty ? "inventory_2" : "production_quantity_limits",
                        IconColor  = empty ? "text-red-600" : "text-orange-500",
                        BgColor    = empty ? "bg-red-50 border-red-200" : "bg-orange-50 border-orange-200",
                        BadgeColor = empty ? "bg-red-600" : "bg-orange-500",
                        Title      = title,
                        Body       = $"الكمية الحالية: {s.StockQuantity} — الحد الأدنى: {s.MinimumStockLevel}",
                        ActionUrl  = "/InventoryIntelligence/PlanningHub",
                        ActionText = "فتح خطة المشتريات",
                        BranchId   = branchId
                    });
            }

            if (!newNotifications.Any()) return;

            _context.SystemNotifications.AddRange(newNotifications);
            await _context.SaveChangesAsync();
        }
    }
}
