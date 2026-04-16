using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PharmaSmartWeb.Controllers
{
    [Authorize]
    public class NotificationsController : BaseController
    {
        public NotificationsController(ApplicationDbContext context) : base(context) { }

        // GET: /Notifications/Index
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "مركز التنبيهات والإشعارات";

            int scopeId = ReportScopeId;
            bool isGlobal = (scopeId == 0);
            var today = DateTime.Today;

            // إحصائيات للعرض في الـ View
            var invQuery = _context.Branchinventory.AsQueryable();
            if (!isGlobal) invQuery = invQuery.Where(bi => bi.BranchId == scopeId);

            ViewBag.ShortagesCount = await invQuery.CountAsync(bi => bi.StockQuantity <= bi.MinimumStockLevel && bi.StockQuantity >= 0);
            ViewBag.ExpiryCount = await _context.DrugBatches
                .CountAsync(b => b.ExpiryDate <= today.AddMonths(2) && b.ExpiryDate >= today);

            return View();
        }

        // GET: /Notifications/GetCount (AJAX للهيدر)
        [HttpGet]
        public async Task<IActionResult> GetCount()
        {
            int scopeId = ReportScopeId;
            bool isGlobal = (scopeId == 0);
            var today = DateTime.Today;

            var invQuery = _context.Branchinventory.AsQueryable();
            if (!isGlobal) invQuery = invQuery.Where(bi => bi.BranchId == scopeId);

            int shortages = await invQuery.CountAsync(bi => bi.StockQuantity <= bi.MinimumStockLevel && bi.StockQuantity >= 0);
            int expiry = await _context.DrugBatches.CountAsync(b => b.ExpiryDate <= today.AddMonths(2) && b.ExpiryDate >= today);

            int total = shortages + expiry;
            return Json(new { count = total, shortages, expiry });
        }
    }
}
