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
    public class ReportCenterController : BaseController
    {
        public ReportCenterController(ApplicationDbContext context) : base(context) { }

        [HttpGet("/ReportCenter")]
        [HttpGet("/ReportCenter/Index")]
        [HasPermission("AccountReports", "View")]
        public async Task<IActionResult> Index()
        {
            int branchId = ReportScopeId;
            bool isGlobalScope = (branchId == 0);

            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var threeMonthsLater = today.AddMonths(3);

            var model = new ReportCenterViewModel();

            var salesQ = _context.Sales.Where(s => s.SaleDate >= startOfMonth);
            if (!isGlobalScope) salesQ = salesQ.Where(s => s.BranchId == branchId);

            model.MonthlyRevenue = await salesQ.SumAsync(s => (decimal?)s.NetAmount) ?? 0m;
            model.TotalTransactions = await salesQ.CountAsync();

            var expQ = _context.Journaldetails
                .Include(j => j.Journal)
                .Include(j => j.Account)
                .Where(j => j.Journal.JournalDate >= startOfMonth && j.Journal.IsPosted == true && j.Account.AccountType != null && j.Account.AccountType.StartsWith("Expense"));
            if (!isGlobalScope) expQ = expQ.Where(j => j.Journal.BranchId == branchId);

            model.MonthlyExpenses = await expQ.SumAsync(j => (decimal?)j.Debit - (decimal?)j.Credit) ?? 0m;

            var expiryQ = _context.Purchasedetails
                .Include(pd => pd.Purchase)
                .Where(pd => pd.RemainingQuantity > 0);
            if (!isGlobalScope) expiryQ = expiryQ.Where(pd => pd.Purchase.BranchId == branchId);

            model.ExpiredItemsCount = await expiryQ.CountAsync(pd => pd.ExpiryDate <= today);
            model.NearExpiryCount = await expiryQ.CountAsync(pd => pd.ExpiryDate > today && pd.ExpiryDate <= threeMonthsLater);

            ViewBag.IsGlobalScope = isGlobalScope;
            return View("~/Views/Report/ReportCenter.cshtml", model);
        }
    }
}
