using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Filters;
using PharmaSmartWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PharmaSmartWeb.Controllers
{
    [Authorize]
    public class CashFlowReportController : BaseController
    {
        public CashFlowReportController(ApplicationDbContext context) : base(context) { }

        [HttpGet("/CashFlowReport")]
        [HttpGet("/CashFlowReport/Index")]
        [HasPermission("AccountReports", "View")]
        public async Task<IActionResult> Index(int? accountId, DateTime? date)
        {
            int branchId = ReportScopeId;
            var targetDate = date?.Date ?? DateTime.Today;
            ViewBag.TargetDate = targetDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

            ViewBag.CashAccounts = await _context.Accounts
                .Where(a => a.IsActive == true && (a.AccountName.Contains("صندوق") || a.AccountName.Contains("بنك") || a.AccountName.Contains("نقد")))
                .OrderBy(a => a.AccountCode)
                .ToListAsync();

            if (accountId == null) return View("~/Views/Report/DailyCashFlow.cshtml", new List<Journaldetails>());

            var selectedAccount = await _context.Accounts.FindAsync(accountId);
            ViewBag.SelectedAccount = selectedAccount;

            if (selectedAccount == null) return View("~/Views/Report/DailyCashFlow.cshtml", new List<Journaldetails>());

            var openingQuery = _context.Journaldetails
                .Include(d => d.Journal)
                .Where(d => d.AccountId == accountId && d.Journal.JournalDate < targetDate && d.Journal.IsPosted == true)
                .AsQueryable();

            if (branchId != 0) openingQuery = openingQuery.Where(d => d.Journal.BranchId == branchId);
            var openingBalance = await openingQuery.SumAsync(d => d.Debit - d.Credit);

            var movementQuery = _context.Journaldetails
                .Include(d => d.Journal)
                .Where(d => d.AccountId == accountId &&
                            d.Journal.JournalDate >= targetDate &&
                            d.Journal.JournalDate < targetDate.AddDays(1) &&
                            d.Journal.IsPosted == true)
                .AsQueryable();

            if (branchId != 0) movementQuery = movementQuery.Where(d => d.Journal.BranchId == branchId);
            var dailyMovements = await movementQuery.OrderBy(d => d.Journal.JournalDate).ToListAsync();

            ViewBag.OpeningBalance = openingBalance;
            ViewBag.TotalDebit = dailyMovements.Sum(m => m.Debit);
            ViewBag.TotalCredit = dailyMovements.Sum(m => m.Credit);
            ViewBag.ClosingBalance = openingBalance + (dailyMovements.Sum(m => m.Debit - m.Credit));

            return View("~/Views/Report/DailyCashFlow.cshtml", dailyMovements);
        }
    }
}
