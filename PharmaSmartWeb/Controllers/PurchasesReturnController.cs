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
    public class PurchasesReturnController : BaseController
    {
        private readonly IAccountingEngine _accountingEngine;

        public PurchasesReturnController(ApplicationDbContext context, IAccountingEngine accountingEngine) : base(context)
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

        [HttpGet]
        [HasPermission("PurchasesReturn", "View")]
        public async Task<IActionResult> Index()
        {
            var returns = await _context.Purchases
                .Include(p => p.User)
                .Include(p => p.Supplier)
                .Where(p => p.BranchId == ActiveBranchId && p.IsReturn == true && p.ParentPurchaseId != null)
                .OrderByDescending(p => p.PurchaseDate)
                .ToListAsync();

            return View(returns);
        }

        [HttpGet]
        [HasPermission("PurchasesReturn", "Add")]
        public async Task<IActionResult> Create(int? id)
        {
            if (id == null) return NotFound();
            var purchase = await _context.Purchases
                .Include(p => p.Supplier)
                .Include(p => p.Purchasedetails).ThenInclude(d => d.Drug)
                .FirstOrDefaultAsync(p => p.PurchaseId == id && p.BranchId == ActiveBranchId);

            if (purchase == null) return NotFound();
            return View(purchase);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("PurchasesReturn", "Add")]
        public async Task<IActionResult> ProcessReturn(int PurchaseId, string ReturnNotes, decimal ReturnedCash, decimal ReturnedBank)
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
                        var originalPurchase = await _context.Purchases
                            .Include(p => p.Purchasedetails).ThenInclude(d => d.Drug)
                            .FirstOrDefaultAsync(p => p.PurchaseId == PurchaseId && p.BranchId == ActiveBranchId);

                        if (originalPurchase == null || originalPurchase.IsReturn == true)
                            throw new Exception("الفاتورة غير صالحة للارتجاع.");

                        var returnPurchase = new Purchases
                        {
                            BranchId = ActiveBranchId,
                            UserId = validUserId,
                            SupplierId = originalPurchase.SupplierId,
                            PurchaseDate = DateTime.Now,
                            TotalAmount = originalPurchase.TotalAmount,
                            Discount = originalPurchase.Discount,
                            TaxAmount = originalPurchase.TaxAmount,
                            NetAmount = originalPurchase.NetAmount,
                            IsReturn = true,
                            ParentPurchaseId = originalPurchase.PurchaseId,
                            Notes = ReturnNotes
                        };

                        _context.Purchases.Add(returnPurchase);
                        await _context.SaveChangesAsync();

                        foreach (var item in originalPurchase.Purchasedetails)
                        {
                            _context.Purchasedetails.Add(new Purchasedetails
                            {
                                PurchaseId = returnPurchase.PurchaseId,
                                DrugId = item.DrugId,
                                Quantity = item.Quantity,
                                CostPrice = item.CostPrice,
                                SellingPrice = item.SellingPrice,
                                BatchNumber = item.BatchNumber,
                                ExpiryDate = item.ExpiryDate,
                                SubTotal = item.SubTotal
                            });

                            var inventory = await _context.Branchinventory.FirstOrDefaultAsync(b => b.DrugId == item.DrugId && b.BranchId == ActiveBranchId);
                            if (inventory != null)
                            {
                                int unitsToRemove = (item.Quantity + item.BonusQuantity) * (item.Drug?.ConversionFactor ?? 1);
                                inventory.StockQuantity -= unitsToRemove; // خصم الكمية من المخزون لأننا أرجعناها للمورد
                                _context.Branchinventory.Update(inventory);
                            }

                            _context.Stockmovements.Add(new Stockmovements
                            {
                                BranchId = ActiveBranchId,
                                DrugId = item.DrugId,
                                MovementDate = DateTime.Now,
                                MovementType = "Purchase Return",
                                Quantity = -item.Quantity,
                                UserId = returnPurchase.UserId,
                                Notes = $"مرتجع فاتورة مورد #{originalPurchase.PurchaseId}"
                            });
                        }

                        await _context.SaveChangesAsync();

                        decimal returnedCredit = originalPurchase.NetAmount - (ReturnedCash + ReturnedBank);
                        if (returnedCredit < 0) returnedCredit = 0;

                        var payload = new AccountingPayload
                        {
                            TransactionType = TransactionType.PurchaseReturn,
                            BranchId = ActiveBranchId,
                            UserId = validUserId,
                            ReferenceNo = returnPurchase.PurchaseId.ToString(),
                            Description = $"مرتجع مشتريات فاتورة #{originalPurchase.InvoiceNumber} - {ReturnNotes}",
                            SupplierId = originalPurchase.SupplierId,
                        };

                        payload.Amounts.Add(AmountSource.NetTotalAmount, originalPurchase.NetAmount);
                        payload.Amounts.Add(AmountSource.PaidCashAmount, ReturnedCash);
                        payload.Amounts.Add(AmountSource.PaidBankAmount, ReturnedBank);
                        payload.Amounts.Add(AmountSource.CreditAmount, returnedCredit);

                        await _accountingEngine.ProcessTransactionAsync(payload);

                        originalPurchase.IsReturn = true;
                        _context.Purchases.Update(originalPurchase);

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                    catch (Exception) { await transaction.RollbackAsync(); throw; }
                });

                await RecordLog("Return", "PurchasesReturn", $"تسجيل مرتجع مشتريات للفاتورة #{PurchaseId}");
                TempData["Success"] = "تم إجراء مرتجع المشتريات وعكس القيود المحاسبية بنجاح.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Create), new { id = PurchaseId });
            }
        }
    }
}