using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Filters;
using PharmaSmartWeb.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PharmaSmartWeb.Controllers
{
    [Authorize]
    public class StockExpiryReportController : BaseController
    {
        public StockExpiryReportController(ApplicationDbContext context) : base(context) { }

        [HttpGet("/StockExpiryReport")]
        [HttpGet("/StockExpiryReport/Index")]
        [HasPermission("AccountReports", "View")]
        public async Task<IActionResult> Index(string filter)
        {
            int branchId = ReportScopeId;
            var today = DateTime.Today;
            var threeMonthsLater = today.AddMonths(3);
            var sixMonthsLater = today.AddMonths(6);

            var query = _context.Purchasedetails
                .Include(d => d.Drug).Include(d => d.Purchase)
                .Where(d => d.RemainingQuantity > 0).AsQueryable();

            if (branchId != 0) query = query.Where(d => d.Purchase.BranchId == branchId);

            if (filter == "expired") query = query.Where(i => i.ExpiryDate <= today);
            else if (filter == "near") query = query.Where(i => i.ExpiryDate > today && i.ExpiryDate <= threeMonthsLater);

            var items = await query.OrderBy(i => i.ExpiryDate)
                .Select(d => new StockExpiryViewModel
                {
                    ItemName = d.Drug.DrugName,
                    Barcode = d.Drug.Barcode,
                    BatchNumber = d.BatchNumber ?? "غير محدد",
                    Quantity = d.RemainingQuantity,
                    UnitName = d.Drug.MainUnit,
                    ExpiryDate = d.ExpiryDate
                }).ToListAsync();

            var statsQuery = _context.Purchasedetails.Include(p => p.Purchase).Where(d => d.RemainingQuantity > 0).AsQueryable();
            if (branchId != 0) statsQuery = statsQuery.Where(d => d.Purchase.BranchId == branchId);

            ViewBag.TotalExpired = await statsQuery.CountAsync(d => d.ExpiryDate <= today);
            ViewBag.TotalNear = await statsQuery.CountAsync(d => d.ExpiryDate > today && d.ExpiryDate <= threeMonthsLater);
            ViewBag.TotalSafe = await statsQuery.CountAsync(d => d.ExpiryDate > sixMonthsLater);
            ViewBag.CurrentFilter = filter;

            return View("~/Views/Report/StockExpiry.cshtml", items);
        }
    }
}
