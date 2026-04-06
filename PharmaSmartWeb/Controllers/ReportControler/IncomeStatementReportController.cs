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
    public class IncomeStatementReportController : BaseController
    {
        public IncomeStatementReportController(ApplicationDbContext context) : base(context) { }

        [HttpGet("/IncomeStatementReport")]
        [HttpGet("/IncomeStatementReport/Index")]
        [HasPermission("AccountReports", "View")]
        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate)
        {
            int branchId = ReportScopeId;
            var start = fromDate?.Date ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var end = toDate?.Date.AddHours(23).AddMinutes(59) ?? DateTime.Now;

            ViewBag.FromDate = start.ToString("yyyy-MM-dd");
            ViewBag.ToDate = end.ToString("yyyy-MM-dd");

            var query = _context.Journaldetails
                .Include(d => d.Journal)
                .Include(d => d.Account)
                .Where(d => d.Journal.JournalDate >= start && d.Journal.JournalDate <= end && d.Journal.IsPosted == true)
                .AsQueryable();

            if (branchId != 0) query = query.Where(d => d.Journal.BranchId == branchId);

            var allData = await query.ToListAsync();

            var salesTotal = allData.Where(d => d.Account.AccountCode.StartsWith("4")).Sum(d => d.Credit - d.Debit);
            var cogsTotal = allData.Where(d => d.Account.AccountCode.StartsWith("511") || d.Account.AccountCode.StartsWith("512")).Sum(d => d.Debit - d.Credit);

            var opExpenses = allData.Where(d => d.Account.AccountCode.StartsWith("5") &&
                                                !d.Account.AccountCode.StartsWith("511") &&
                                                !d.Account.AccountCode.StartsWith("512"))
                .GroupBy(d => new { d.Account.AccountCode, d.Account.AccountName })
                .Select(g => new PnlAccountViewModel
                {
                    Name = g.Key.AccountName,
                    Code = g.Key.AccountCode,
                    Total = g.Sum(x => x.Debit - x.Credit),
                    Percentage = salesTotal > 0 ? (double)(g.Sum(x => x.Debit - x.Credit) / salesTotal * 100) : 0
                }).ToList();

            ViewBag.Sales = salesTotal;
            ViewBag.COGS = cogsTotal;
            ViewBag.GrossProfit = salesTotal - cogsTotal;
            ViewBag.OperatingExpenses = opExpenses;
            ViewBag.TotalOpExpenses = opExpenses.Sum(e => e.Total);
            ViewBag.NetIncome = (decimal)ViewBag.GrossProfit - (decimal)ViewBag.TotalOpExpenses;

            ViewBag.GrossMargin = salesTotal > 0 ? (ViewBag.GrossProfit / salesTotal * 100) : 0;
            ViewBag.NetMargin = salesTotal > 0 ? (ViewBag.NetIncome / salesTotal * 100) : 0;

            return View("~/Views/Report/IncomeStatement.cshtml");
        }
    }
}
