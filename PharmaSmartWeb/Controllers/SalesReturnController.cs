using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Models;
using PharmaSmartWeb.Filters;
using PharmaSmartWeb.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PharmaSmartWeb.Controllers
{
    [Authorize]
    public class SalesReturnController : BaseController
    {
        private readonly IAccountingEngine _accountingEngine;

        public SalesReturnController(ApplicationDbContext context, IAccountingEngine accountingEngine) : base(context)
        {
            _accountingEngine = accountingEngine;
        }

        private async Task<int> GetValidUserIdAsync()
        {
            var userIdClaim = User.FindFirst("UserID")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int parsedId))
            {
                if (await _context.Users.AnyAsync(u => u.UserId == parsedId)) return parsedId;
            }
            throw new Exception("انتهت صلاحية الجلسة أو تعذر التحقق من هوية المستخدم. يرجى تسجيل الدخول مجدداً.");
        }

        // ==========================================
        // 🔙 1. سجل المرتجعات (تم توحيد الاسم إلى Index)
        // ==========================================
        [HttpGet]
        [HasPermission("SalesReturn", "View")]
        public async Task<IActionResult> Index()
        {
            var returns = await _context.Sales
                .Include(s => s.User)
                .Include(s => s.Customer)
                .Where(s => s.BranchId == ActiveBranchId && s.IsReturn == true && s.ParentSaleId != null)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            return View(returns);
        }

        // ==========================================
        // 🔙 2. شاشة المرتجع لفاتورة 
        // ==========================================
        [HttpGet]
        [HasPermission("SalesReturn", "Add")]
        public async Task<IActionResult> Create(int? id)
        {
            if (id == null) return NotFound();
            var sale = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.Saledetails).ThenInclude(d => d.Drug)
                .Include(s => s.SalePayments)
                .FirstOrDefaultAsync(s => s.SaleId == id && s.BranchId == ActiveBranchId);

            if (sale == null) return NotFound();
            return View(sale);
        }

        // ==========================================
        // 🔄 3. معالجة المرتجع (عكس مالي ومخزني)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("SalesReturn", "Add")]
        public async Task<IActionResult> ProcessReturn(int SaleId, string ReturnNotes)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        var validUserId = await GetValidUserIdAsync();
                        var originalSale = await _context.Sales.Include(s => s.Saledetails).Include(s => s.SalePayments).FirstOrDefaultAsync(s => s.SaleId == SaleId && s.BranchId == ActiveBranchId);
                        if (originalSale == null || originalSale.IsReturn == true) throw new Exception("الفاتورة غير صالحة للارتجاع.");

                        var returnSale = new Sales
                        {
                            BranchId = ActiveBranchId,
                            UserId = validUserId,
                            CustomerId = originalSale.CustomerId,
                            SaleDate = DateTime.Now,
                            TotalAmount = originalSale.TotalAmount,
                            Discount = originalSale.Discount,
                            TaxAmount = originalSale.TaxAmount,
                            NetAmount = originalSale.NetAmount,
                            IsReturn = true,
                            ParentSaleId = originalSale.SaleId
                        };
                        _context.Sales.Add(returnSale);
                        await _context.SaveChangesAsync();

                        decimal totalCogsReversed = 0;

                        foreach (var item in originalSale.Saledetails)
                        {
                            _context.Saledetails.Add(new Saledetails { SaleId = returnSale.SaleId, DrugId = item.DrugId, Quantity = item.Quantity, UnitPrice = item.UnitPrice });

                            var inventory = await _context.Branchinventory.FirstOrDefaultAsync(b => b.DrugId == item.DrugId && b.BranchId == ActiveBranchId);
                            if (inventory != null)
                            {
                                inventory.StockQuantity += item.Quantity;
                                totalCogsReversed += (item.Quantity * (inventory.AverageCost ?? 0));
                                _context.Branchinventory.Update(inventory);
                            }

                            _context.Stockmovements.Add(new Stockmovements { BranchId = ActiveBranchId, DrugId = item.DrugId, MovementDate = DateTime.Now, MovementType = "Sales Return", Quantity = item.Quantity, UserId = returnSale.UserId, Notes = $"مرتجع للفاتورة #{originalSale.SaleId}" });
                        }

                        await _context.SaveChangesAsync();

                        decimal returnedCash = originalSale.SalePayments.Where(p => p.PaymentMethod == "Cash").Sum(p => p.Amount);
                        decimal returnedBank = originalSale.SalePayments.Where(p => p.PaymentMethod == "Bank").Sum(p => p.Amount);
                        decimal returnedCredit = originalSale.SalePayments.Where(p => p.PaymentMethod == "Credit").Sum(p => p.Amount);

                        var payload = new AccountingPayload
                        {
                            TransactionType = TransactionType.SalesReturn,
                            BranchId = ActiveBranchId,
                            UserId = validUserId,
                            ReferenceNo = returnSale.SaleId.ToString(),
                            Description = $"مرتجع مبيعات فاتورة #{originalSale.SaleId} - {ReturnNotes}",
                            CustomerId = originalSale.CustomerId,
                            SpecificCashAccountId = originalSale.SalePayments.FirstOrDefault(p => p.PaymentMethod == "Cash")?.AccountId,
                            SpecificBankAccountId = originalSale.SalePayments.FirstOrDefault(p => p.PaymentMethod == "Bank")?.AccountId
                        };

                        payload.Amounts.Add(AmountSource.NetTotalAmount, originalSale.NetAmount);
                        payload.Amounts.Add(AmountSource.PaidCashAmount, returnedCash);
                        payload.Amounts.Add(AmountSource.PaidBankAmount, returnedBank);
                        payload.Amounts.Add(AmountSource.CreditAmount, returnedCredit);
                        payload.Amounts.Add(AmountSource.COGSAmount, totalCogsReversed);

                        await _accountingEngine.ProcessTransactionAsync(payload);

                        originalSale.IsReturn = true;
                        _context.Sales.Update(originalSale);

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        await transaction.RollbackAsync();
                        throw new Exception("تم إجراء مرتجع لهذه الفاتورة للتو من قبل مستخدم آخر (عملية متزامنة)! تم إحباط العملية لضمان صحة الأرصدة والمخزون.");
                    }
                    catch (Exception) { await transaction.RollbackAsync(); throw; }
                });

                await RecordLog("Return", "SalesReturn", $"تسجيل مرتجع مبيعات للفاتورة #{SaleId}");
                TempData["Success"] = "تم إجراء المرتجع وعكس القيود المحاسبية بنجاح.";

                // 🚀 التوجيه الصحيح لمنع خطأ 404
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                // 🚀 التوجيه الصحيح لمنع خطأ 404
                return RedirectToAction(nameof(Create), new { id = SaleId });
            }
        }
    }
}