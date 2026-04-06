using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Models;
using PharmaSmartWeb.Filters;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PharmaSmartWeb.Controllers
{
    // =======================================================
    // 💡 ViewModel لربط شاشة التوجيه المحاسبي بالمحرك الجديد
    // =======================================================
    public class FinancialMappingViewModel
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; }
        public int? CashAccountId { get; set; }
        public int? BankAccountId { get; set; }
        public int? SalesRevenueAccountId { get; set; }
        public int? COGSAccountId { get; set; }
        public int? InventoryAccountId { get; set; }
    }

    [Authorize]
    public class FinancialSettingsController : BaseController
    {
        public FinancialSettingsController(ApplicationDbContext context) : base(context) { }

        // ==========================================
        // ⚙️ 1. عرض شاشة التوجيه المحاسبي للفرع النشط
        // ==========================================
        [HttpGet]
        [HasPermission("Settings", "View")]
        public async Task<IActionResult> Index()
        {
            if (ActiveBranchId <= 0) return RedirectToAction("Index", "Home");

            var branch = await _context.Branches.FindAsync(ActiveBranchId);
            if (branch == null) return NotFound();

            // جلب المابس (Mappings) الحالية من محرك التوجيه المحاسبي للفرع
            var mappings = await _context.AccountMappings
                .Where(m => m.BranchId == ActiveBranchId)
                .ToListAsync();

            // تعبئة الـ ViewModel
            var model = new FinancialMappingViewModel
            {
                BranchId = branch.BranchId,
                BranchName = branch.BranchName,
                CashAccountId = mappings.FirstOrDefault(m => m.Role == AccountRole.Cash)?.AccountId,
                BankAccountId = mappings.FirstOrDefault(m => m.Role == AccountRole.Bank)?.AccountId,
                SalesRevenueAccountId = mappings.FirstOrDefault(m => m.Role == AccountRole.SalesRevenue)?.AccountId,
                COGSAccountId = mappings.FirstOrDefault(m => m.Role == AccountRole.COGS)?.AccountId,
                InventoryAccountId = mappings.FirstOrDefault(m => m.Role == AccountRole.Inventory)?.AccountId
            };

            await PrepareAccountDropdowns();
            return View(model);
        }

        // ==========================================
        // 💾 2. حفظ إعدادات التوجيه (في جدول AccountMappings)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Settings", "Edit")]
        public async Task<IActionResult> Index(FinancialMappingViewModel model)
        {
            if (model.BranchId != ActiveBranchId) return NotFound();

            try
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        // تحديث أو إنشاء الروابط المحاسبية (Dynamic Mappings)
                        await UpdateOrCreateMapping(ActiveBranchId, AccountRole.Cash, model.CashAccountId);
                        await UpdateOrCreateMapping(ActiveBranchId, AccountRole.Bank, model.BankAccountId);
                        await UpdateOrCreateMapping(ActiveBranchId, AccountRole.SalesRevenue, model.SalesRevenueAccountId);
                        await UpdateOrCreateMapping(ActiveBranchId, AccountRole.COGS, model.COGSAccountId);
                        await UpdateOrCreateMapping(ActiveBranchId, AccountRole.Inventory, model.InventoryAccountId);

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                    catch (Exception) { await transaction.RollbackAsync(); throw; }
                });

                await RecordLog("Update", "Settings", $"تحديث خريطة التوجيه المحاسبي الديناميكي للفرع: {model.BranchName}");
                TempData["Success"] = "تم حفظ التوجيه المحاسبي بنجاح. محرك الـ ERP سيقوم الآن بتوليد القيود بناءً على هذه الإعدادات.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Error = "حدث خطأ أثناء حفظ التوجيهات: " + ex.Message;
            }

            await PrepareAccountDropdowns();
            return View(model);
        }

        // ==========================================
        // 🛠️ دالة مساعدة لحفظ المابس (Upsert)
        // ==========================================
        private async Task UpdateOrCreateMapping(int branchId, AccountRole role, int? accountId)
        {
            var existingMapping = await _context.AccountMappings
                .FirstOrDefaultAsync(m => m.BranchId == branchId && m.Role == role);

            if (accountId.HasValue && accountId.Value > 0)
            {
                if (existingMapping == null)
                {
                    _context.AccountMappings.Add(new AccountMapping { BranchId = branchId, Role = role, AccountId = accountId.Value });
                }
                else
                {
                    existingMapping.AccountId = accountId.Value;
                    _context.AccountMappings.Update(existingMapping);
                }
            }
            else if (existingMapping != null)
            {
                // إذا ترك المستخدم الحقل فارغاً، نحذف التوجيه
                _context.AccountMappings.Remove(existingMapping);
            }
        }

        // ==========================================
        // 🛠️ دالة فلترة الحسابات للقوائم المنسدلة
        // ==========================================
        private async Task PrepareAccountDropdowns()
        {
            var activeAccounts = await _context.Accounts
                .Where(a => a.IsActive == true && a.IsParent == false)
                .ToListAsync();

            // 1. الصناديق والبنوك
            ViewBag.CashAccounts = new SelectList(activeAccounts.Where(a => a.AccountName.Contains("صندوق") || a.AccountName.Contains("نقد")), "AccountId", "AccountName");
            ViewBag.BankAccounts = new SelectList(activeAccounts.Where(a => a.AccountName.Contains("بنك") || a.AccountName.Contains("مصرف") || a.AccountName.Contains("حساب")), "AccountId", "AccountName");

            // 2. إيرادات المبيعات
            ViewBag.SalesAccounts = new SelectList(activeAccounts.Where(a => a.AccountType == "Revenue"), "AccountId", "AccountName");

            // 3. تكلفة المبيعات COGS
            var cogsAccounts = activeAccounts.Where(a => a.AccountType == "Expenses" && (a.AccountName.Contains("تكلفة") || a.AccountName.Contains("مبيعات") || a.AccountName.Contains("COGS"))).ToList();
            if (!cogsAccounts.Any()) cogsAccounts = activeAccounts.Where(a => a.AccountType == "Expenses").ToList();
            ViewBag.COGSAccounts = new SelectList(cogsAccounts, "AccountId", "AccountName");

            // 4. المخزون (أصول)
            var inventoryAccounts = activeAccounts.Where(a => a.AccountType == "Assets" && (a.AccountName.Contains("مخزون") || a.AccountName.Contains("بضاعة"))).ToList();
            if (!inventoryAccounts.Any()) inventoryAccounts = activeAccounts.Where(a => a.AccountType == "Assets").ToList();
            ViewBag.InventoryAccounts = new SelectList(inventoryAccounts, "AccountId", "AccountName");
        }
    }
}