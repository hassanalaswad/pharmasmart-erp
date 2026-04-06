using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Filters;
using PharmaSmartWeb.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PharmaSmartWeb.Controllers.ReportControler
{
    [Authorize]
    public class PharmacistSalesReportController : BaseController
    {
        public PharmacistSalesReportController(ApplicationDbContext context) : base(context) { }

        [HttpGet("/PharmacistSalesReport")]
        [HttpGet("/PharmacistSalesReport/Index")]
        [HasPermission("AccountReports", "View")]
        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate, decimal commissionRate = 1)
        {
            int branchId = ReportScopeId;
            var start = fromDate?.Date ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var end = toDate?.Date.AddHours(23).AddMinutes(59) ?? DateTime.Now;

            ViewBag.FromDate = start.ToString("yyyy-MM-dd");
            ViewBag.ToDate = end.ToString("yyyy-MM-dd");
            ViewBag.CommissionRate = commissionRate;

            var query = _context.Sales
                .Include(s => s.User)
                .Where(s => s.SaleDate >= start && s.SaleDate <= end)
                .AsQueryable();

            if (branchId != 0) query = query.Where(s => s.BranchId == branchId);

            var salesData = await query
                .GroupBy(s => new { s.UserId, s.User.Username })
                .Select(g => new PharmacistSalesViewModel
                {
                    UserId = g.Key.UserId,
                    Username = g.Key.Username,
                    InvoiceCount = g.Count(),
                    TotalSales = g.Sum(s => s.TotalAmount),
                    CommissionAmount = g.Sum(s => s.TotalAmount) * (commissionRate / 100)
                })
                .OrderByDescending(x => x.TotalSales)
                .ToListAsync();

            decimal grandTotalSales = salesData.Sum(x => x.TotalSales);
            foreach (var item in salesData)
            {
                item.SalesPercentage = grandTotalSales > 0 ? (double)(item.TotalSales / grandTotalSales * 100) : 0;
            }

            ViewBag.GrandTotalSales = grandTotalSales;
            ViewBag.TotalCommissions = salesData.Sum(x => x.CommissionAmount);
            ViewBag.TopSeller = salesData.FirstOrDefault()?.Username ?? "---";

            return View("~/Views/Report/PharmacistSales.cshtml", salesData);
        }
    }
}
