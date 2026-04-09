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
    public class ProfitLossReportController : BaseController
    {
        public ProfitLossReportController(ApplicationDbContext context) : base(context) { }

        [HttpGet("/ProfitLossReport")]
        [HttpGet("/ProfitLossReport/Index")]
        [HasPermission("AccountReports", "View")]
        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate)
        {
            int branchId = ReportScopeId;
            var start = fromDate?.Date ?? new DateTime(DateTime.Now.Year, 1, 1);
            var end = toDate?.Date.AddHours(23).AddMinutes(59) ?? DateTime.Now;

            ViewBag.FromDate = start.ToString("yyyy-MM-dd");
            ViewBag.ToDate = end.ToString("yyyy-MM-dd");

            var query = _context.Journaldetails
                .Include(d => d.Journal)
                .Include(d => d.Account)
                .Where(d => d.Journal.JournalDate >= start &&
                            d.Journal.JournalDate <= end &&
                            d.Journal.IsPosted == true &&
                            (d.Account.AccountType == "Revenue" || (d.Account.AccountType != null && d.Account.AccountType.StartsWith("Expense"))))
                .AsQueryable();

            if (branchId != 0) query = query.Where(d => d.Journal.BranchId == branchId);

            var pnlData = await query.ToListAsync();

            var revenueAccounts = pnlData
                .Where(d => d.Account.AccountType == "Revenue")
                .GroupBy(d => new { d.Account.AccountCode, d.Account.AccountName })
                .Select(g => new PnlAccountViewModel
                {
                    Name = g.Key.AccountName,
                    Code = g.Key.AccountCode,
                    Total = g.Sum(x => x.Credit - x.Debit)
                }).ToList();

            var expenseAccounts = pnlData
                .Where(d => d.Account.AccountType != null && d.Account.AccountType.StartsWith("Expense"))
                .GroupBy(d => new { d.Account.AccountCode, d.Account.AccountName })
                .Select(g => new PnlAccountViewModel
                {
                    Name = g.Key.AccountName,
                    Code = g.Key.AccountCode,
                    Total = g.Sum(x => x.Debit - x.Credit)
                }).ToList();

            decimal totalRevenue = revenueAccounts.Sum(r => r.Total);
            decimal totalExpenses = expenseAccounts.Sum(e => e.Total);

            ViewBag.Revenues = revenueAccounts;
            ViewBag.Expenses = expenseAccounts;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalExpenses = totalExpenses;
            ViewBag.NetProfit = totalRevenue - totalExpenses;

            ViewBag.ExpenseLabels = expenseAccounts.Select(e => e.Name).ToArray();
            ViewBag.ExpenseValues = expenseAccounts.Select(e => e.Total).ToArray();
            ViewBag.ComparisonLabels = new string[] { "إجمالي الإيرادات", "إجمالي المصروفات" };
            ViewBag.ComparisonValues = new decimal[] { totalRevenue, totalExpenses };

            return View("~/Views/Report/ProfitAndLoss.cshtml");
        }
    }
}
