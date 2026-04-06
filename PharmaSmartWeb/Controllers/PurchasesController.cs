//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.EntityFrameworkCore;
//using PharmaSmartWeb.Models;
//using PharmaSmartWeb.Filters;
//using System;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Collections.Generic;
//using Microsoft.AspNetCore.Authorization;
//using PharmaSmartWeb.Services;

//namespace PharmaSmartWeb.Controllers
//{
//    [Authorize]
//    public class PurchasesController : BaseController
//    {
//        private readonly IAccountingEngine _accountingEngine;

//        public PurchasesController(ApplicationDbContext context, IAccountingEngine accountingEngine) : base(context)
//        {
//            _accountingEngine = accountingEngine;
//        }

//        private async Task<int> GetValidUserIdAsync()
//        {
//            var userIdClaim = User.FindFirst("UserID")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
//            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int parsedId))
//            {
//                if (await _context.Users.AnyAsync(u => u.UserId == parsedId)) return parsedId;
//            }
//            var fallbackUser = await _context.Users.FirstOrDefaultAsync();
//            if (fallbackUser == null) throw new Exception("لا يوجد مستخدم مسجل لربط العملية به!");
//            return fallbackUser.UserId;
//        }

//        [HttpGet]
//        [HasPermission("Purchases", "View")]
//        public async Task<IActionResult> Index()
//        {
//            int currentBranchId = ActiveBranchId;
//            var purchases = await _context.Purchases
//                .Include(p => p.Supplier)
//                .Where(p => p.BranchId == currentBranchId)
//                .OrderByDescending(p => p.PurchaseDate)
//                .ToListAsync();

//            return View(purchases);
//        }

//        [HttpGet]
//        [HasPermission("Purchases", "View")]
//        public async Task<IActionResult> Details(int? id)
//        {
//            if (id == null) return NotFound();
//            int currentBranchId = ActiveBranchId;

//            var purchase = await _context.Purchases
//                .Include(p => p.Supplier).Include(p => p.User)
//                .Include(p => p.Purchasedetails).ThenInclude(d => d.Drug)
//                .FirstOrDefaultAsync(m => m.PurchaseId == id && m.BranchId == currentBranchId);

//            if (purchase == null) return NotFound();
//            return View(purchase);
//        }

//        [HttpGet]
//        [HasPermission("Purchases", "Add")]
//        public IActionResult Create()
//        {
//            PrepareDropdowns();
//            return View(new Purchases { PurchaseDate = DateTime.Now, AmountPaid = 0 });
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        [HasPermission("Purchases", "Add")]
//        public async Task<IActionResult> Create(Purchases purchase, decimal CashAmount, int? CashAccountId, decimal BankAmount, int? BankAccountId)
//        {
//            int currentBranchId = ActiveBranchId;
//            ModelState.Remove("Supplier"); ModelState.Remove("User"); ModelState.Remove("Branch"); ModelState.Remove("PaymentStatus");

//            if (purchase.Purchasedetails != null)
//            {
//                var detailsList = purchase.Purchasedetails.ToList();
//                for (int i = 0; i < detailsList.Count; i++) { ModelState.Remove($"Purchasedetails[{i}].Purchase"); ModelState.Remove($"Purchasedetails[{i}].Drug"); }
//            }

//            if (!ModelState.IsValid)
//            {
//                var errors = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
//                ViewBag.Error = "رفض النظام بسبب أخطاء في البيانات المكتوبة: " + errors;
//                return ReloadCreateView(purchase);
//            }

//            if (purchase.Purchasedetails == null || !purchase.Purchasedetails.Any())
//            {
//                ViewBag.Error = "يجب إضافة صنف واحد على الأقل للفاتورة.";
//                return ReloadCreateView(purchase);
//            }

//            var supplierCheck = await _context.Suppliers.FindAsync(purchase.SupplierId);
//            if (supplierCheck == null || supplierCheck.AccountId == null || supplierCheck.AccountId == 0)
//            {
//                ViewBag.Error = $"المورد المختار ({supplierCheck?.SupplierName}) غير مربوط بحساب مالي في الدليل المحاسبي. يرجى الدخول لشاشة الموردين وربطه بحساب.";
//                return ReloadCreateView(purchase);
//            }

//            if (CashAmount > 0 && (CashAccountId == null || CashAccountId <= 0))
//            {
//                ViewBag.Error = "لقد قمت بإدخال مبلغ نقدي، يرجى اختيار الصندوق المراد السحب منه.";
//                return ReloadCreateView(purchase);
//            }

//            if (BankAmount > 0 && (BankAccountId == null || BankAccountId <= 0))
//            {
//                ViewBag.Error = "لقد قمت بإدخال مبلغ حوالة، يرجى اختيار البنك المراد السحب منه.";
//                return ReloadCreateView(purchase);
//            }
//            // 🛡️ حاجز أمني: التحقق من عدم تكرار رقم الفاتورة لنفس المورد في نفس الفرع
//            bool isDuplicateInvoice = await _context.Purchases.AnyAsync(p =>
//                p.SupplierId == purchase.SupplierId &&
//                p.InvoiceNumber == purchase.InvoiceNumber &&
//                p.BranchId == currentBranchId);

//            if (isDuplicateInvoice)
//            {
//                var supplier = await _context.Suppliers.FindAsync(purchase.SupplierId);
//                ViewBag.Error = $"عفواً، رقم الفاتورة ({purchase.InvoiceNumber}) مسجل مسبقاً للمورد ({supplier?.SupplierName}). يرجى التأكد من الرقم.";
//                return ReloadCreateView(purchase);
//            }

//            var strategy = _context.Database.CreateExecutionStrategy();
//            try
//            {
//                await strategy.ExecuteAsync(async () =>
//                {
//                    using var transaction = await _context.Database.BeginTransactionAsync();
//                    try
//                    {
//                        decimal calculatedTotal = purchase.Purchasedetails.Sum(i => i.Quantity * i.CostPrice);
//                        decimal calculatedNet = calculatedTotal - purchase.Discount + purchase.TaxAmount;
//                        decimal totalBonusValue = purchase.Purchasedetails.Sum(i => i.BonusQuantity * i.CostPrice);

//                        purchase.AmountPaid = CashAmount + BankAmount;
//                        if (purchase.AmountPaid > calculatedNet) purchase.AmountPaid = calculatedNet;

//                        purchase.RemainingAmount = calculatedNet - purchase.AmountPaid;

//                        purchase.UserId = await GetValidUserIdAsync();
//                        purchase.BranchId = currentBranchId;
//                        purchase.PurchaseDate = DateTime.Now;
//                        purchase.PaymentStatus = purchase.RemainingAmount <= 0.01m ? "Paid" : (purchase.AmountPaid > 0 ? "Partial" : "Unpaid");
//                        purchase.TotalAmount = calculatedTotal;
//                        purchase.NetAmount = calculatedNet;
//                        purchase.CreatedAt = DateTime.Now;

//                        // 🚀 الحل: تنظيف البيانات وحل مشكلة الـ Null قبل الحفظ في قاعدة البيانات
//                        foreach (var item in purchase.Purchasedetails)
//                        {
//                            if (string.IsNullOrWhiteSpace(item.BatchNumber))
//                            {
//                                item.BatchNumber = "N/A"; // وضع قيمة افتراضية لتجنب خطأ قاعدة البيانات
//                            }
//                            item.SubTotal = item.Quantity * item.CostPrice;
//                            item.RemainingQuantity = item.Quantity + item.BonusQuantity;
//                        }

//                        // حفظ الفاتورة وتفاصيلها بأمان
//                        _context.Purchases.Add(purchase);
//                        await _context.SaveChangesAsync();

//                        // استكمال باقي العمليات (مخزون، باركود، باتشات)
//                        foreach (var item in purchase.Purchasedetails)
//                        {
//                            var drug = await _context.Drugs.FindAsync(item.DrugId);
//                            if (drug != null)
//                            {
//                                int totalUnitsToAdd = (item.Quantity + item.BonusQuantity) * drug.ConversionFactor;
//                                decimal totalCostPaid = item.Quantity * item.CostPrice;

//                                if (item.BatchNumber != "N/A")
//                                {
//                                    var existingBatch = await _context.DrugBatches.FirstOrDefaultAsync(b => b.DrugId == item.DrugId && b.BatchNumber == item.BatchNumber);
//                                    if (existingBatch == null) _context.DrugBatches.Add(new DrugBatches { DrugId = item.DrugId, BatchNumber = item.BatchNumber, ExpiryDate = item.ExpiryDate });
//                                }

//                                var inventory = await _context.Branchinventory.FirstOrDefaultAsync(b => b.DrugId == item.DrugId && b.BranchId == currentBranchId);
//                                if (inventory != null)
//                                {
//                                    decimal currentQty = inventory.StockQuantity > 0 ? inventory.StockQuantity : 0;
//                                    decimal currentCost = inventory.AverageCost ?? 0m;
//                                    decimal newTotalQty = currentQty + totalUnitsToAdd;

//                                    if (newTotalQty > 0) inventory.AverageCost = ((currentQty * currentCost) + totalCostPaid) / newTotalQty;

//                                    inventory.StockQuantity += totalUnitsToAdd;
//                                    inventory.CurrentSellingPrice = item.SellingPrice;
//                                    _context.Branchinventory.Update(inventory);
//                                }
//                                else
//                                {
//                                    decimal newUnitCost = totalUnitsToAdd > 0 ? (totalCostPaid / totalUnitsToAdd) : 0;
//                                    inventory = new Branchinventory { DrugId = item.DrugId, BranchId = currentBranchId, StockQuantity = totalUnitsToAdd, MinimumStockLevel = 10, AverageCost = newUnitCost, CurrentSellingPrice = item.SellingPrice };
//                                    _context.Branchinventory.Add(inventory);
//                                }

//                                _context.Stockmovements.Add(new Stockmovements { DrugId = item.DrugId, BranchId = currentBranchId, MovementType = "Purchase In", Quantity = totalUnitsToAdd, MovementDate = DateTime.Now, ReferenceId = purchase.PurchaseId, UserId = purchase.UserId, Notes = $"توريد - فاتورة {purchase.InvoiceNumber}" });

//                                string generatedCode = $"{item.DrugId}-{item.BatchNumber}-{item.ExpiryDate:yyMM}-{(int)item.SellingPrice}";
//                                _context.BarcodeGenerator.Add(new BarcodeGenerator { BranchId = currentBranchId, DrugId = item.DrugId, BatchNumber = item.BatchNumber, ExpiryDate = item.ExpiryDate, CurrentPrice = item.SellingPrice, QuantityToPrint = totalUnitsToAdd, GeneratedCode = generatedCode, IsPrinted = false, CreatedAt = DateTime.Now, UserId = purchase.UserId });
//                            }
//                        }

//                        await _context.SaveChangesAsync();

//                        var payload = new AccountingPayload
//                        {
//                            TransactionType = TransactionType.PurchaseInvoice,
//                            BranchId = currentBranchId,
//                            UserId = purchase.UserId,
//                            ReferenceNo = purchase.InvoiceNumber,
//                            Description = $"فاتورة مشتريات #{purchase.InvoiceNumber} - المورد: {supplierCheck.SupplierName}",
//                            SupplierId = purchase.SupplierId,
//                            SpecificCashAccountId = CashAmount > 0 ? CashAccountId : null,
//                            SpecificBankAccountId = BankAmount > 0 ? BankAccountId : null
//                        };

//                        payload.Amounts.Add(AmountSource.NetTotalAmount, calculatedNet + totalBonusValue);
//                        payload.Amounts.Add(AmountSource.PaidCashAmount, CashAmount);
//                        payload.Amounts.Add(AmountSource.PaidBankAmount, BankAmount);
//                        payload.Amounts.Add(AmountSource.CreditAmount, purchase.RemainingAmount);
//                        payload.Amounts.Add(AmountSource.BonusAmount, totalBonusValue);
//                        payload.Amounts.Add(AmountSource.TaxAmount, purchase.TaxAmount);
//                        payload.Amounts.Add(AmountSource.Discount, purchase.Discount);

//                        await _accountingEngine.ProcessTransactionAsync(payload);

//                        await transaction.CommitAsync();
//                    }
//                    catch (Exception ex)
//                    {
//                        await transaction.RollbackAsync();
//                        throw new Exception(ex.Message + (ex.InnerException != null ? " - " + ex.InnerException.Message : ""));
//                    }
//                });

//                await RecordLog("Add", "Purchases", $"فاتورة توريد #{purchase.InvoiceNumber} (مدفوع: {purchase.AmountPaid})");
//                TempData["Success"] = "تم حفظ الفاتورة وتحديث المخزون وتوليد القيد المحاسبي بنجاح.";
//                return RedirectToAction(nameof(Index));
//            }
//            catch (Exception ex)
//            {
//                ViewBag.Error = "النظام رفض الحفظ للسبب التالي: " + ex.Message;
//                return ReloadCreateView(purchase);
//            }
//        }

//        private IActionResult ReloadCreateView(Purchases purchase)
//        {
//            PrepareDropdowns(purchase.SupplierId);

//            if (purchase.Purchasedetails != null && purchase.Purchasedetails.Any())
//            {
//                var recoveredCart = new List<object>();
//                foreach (var item in purchase.Purchasedetails)
//                {
//                    var drugName = _context.Drugs.Where(d => d.DrugId == item.DrugId).Select(d => d.DrugName).FirstOrDefault() ?? "صنف غير معروف";
//                    recoveredCart.Add(new
//                    {
//                        uid = DateTime.Now.Ticks + item.DrugId,
//                        id = item.DrugId,
//                        name = drugName,
//                        qty = item.Quantity,
//                        bonus = item.BonusQuantity,
//                        cost = item.CostPrice,
//                        sell = item.SellingPrice,
//                        batch = item.BatchNumber ?? "",
//                        expiry = item.ExpiryDate.ToString("yyyy-MM-dd")
//                    });
//                }
//                ViewBag.RecoveredCartJson = System.Text.Json.JsonSerializer.Serialize(recoveredCart);
//            }

//            return View(purchase);
//        }

//        [HttpGet]
//        [HasPermission("Purchases", "Edit")]
//        public async Task<IActionResult> Edit(int? id)
//        {
//            if (id == null) return NotFound();
//            int currentBranchId = ActiveBranchId;

//            var purchase = await _context.Purchases.Include(p => p.Purchasedetails).FirstOrDefaultAsync(p => p.PurchaseId == id && p.BranchId == currentBranchId);
//            if (purchase == null) return NotFound();
//            return ReloadEditView(purchase);
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        [HasPermission("Purchases", "Edit")]
//        public async Task<IActionResult> Edit(int id, Purchases purchase, int? PaidFromAccountId)
//        {
//            if (id != purchase.PurchaseId) return NotFound();
//            int currentBranchId = ActiveBranchId;

//            ModelState.Remove("Supplier"); ModelState.Remove("User"); ModelState.Remove("Branch"); ModelState.Remove("PaymentStatus");

//            if (purchase.Purchasedetails != null)
//            {
//                var detailsList = purchase.Purchasedetails.ToList();
//                for (int i = 0; i < detailsList.Count; i++) { ModelState.Remove($"Purchasedetails[{i}].Purchase"); ModelState.Remove($"Purchasedetails[{i}].Drug"); }
//            }

//            if (ModelState.IsValid)
//            {
//                decimal calculatedNet = purchase.Purchasedetails.Sum(i => i.Quantity * i.CostPrice) - purchase.Discount + purchase.TaxAmount;

//                if (purchase.PaymentStatus == "Paid") purchase.AmountPaid = calculatedNet;
//                else purchase.AmountPaid = 0;

//                purchase.RemainingAmount = calculatedNet - purchase.AmountPaid;

//                if (purchase.AmountPaid > 0 && PaidFromAccountId is int paidAccId && paidAccId > 0)
//                {
//                    decimal actualBalance = await CalculateAccountBalance(paidAccId);
//                    var oldPurchase = await _context.Purchases.AsNoTracking().FirstOrDefaultAsync(p => p.PurchaseId == id);
//                    if (oldPurchase != null) actualBalance += oldPurchase.AmountPaid;

//                    if (actualBalance < purchase.AmountPaid)
//                    {
//                        ViewBag.Error = $"عفواً، الرصيد المتاح لتعديل الفاتورة لا يكفي.";
//                        return ReloadEditView(purchase);
//                    }
//                }

//                var strategy = _context.Database.CreateExecutionStrategy();
//                try
//                {
//                    await strategy.ExecuteAsync(async () =>
//                    {
//                        using var transaction = await _context.Database.BeginTransactionAsync();
//                        try
//                        {
//                            var existingPurchase = await _context.Purchases.Include(p => p.Purchasedetails).FirstOrDefaultAsync(p => p.PurchaseId == id && p.BranchId == currentBranchId);
//                            if (existingPurchase == null) throw new Exception("الفاتورة غير موجودة.");

//                            foreach (var oldItem in existingPurchase.Purchasedetails)
//                            {
//                                var drug = await _context.Drugs.FindAsync(oldItem.DrugId);
//                                if (drug != null)
//                                {
//                                    var inventory = await _context.Branchinventory.FirstOrDefaultAsync(b => b.DrugId == oldItem.DrugId && b.BranchId == existingPurchase.BranchId);
//                                    if (inventory != null)
//                                    {
//                                        int oldUnits = (oldItem.Quantity + oldItem.BonusQuantity) * drug.ConversionFactor;
//                                        decimal oldTotalCost = (oldItem.Quantity + oldItem.BonusQuantity) * oldItem.CostPrice;
//                                        decimal qtyAfterReversal = inventory.StockQuantity - oldUnits;
//                                        inventory.AverageCost = qtyAfterReversal > 0 ? ((inventory.StockQuantity * (inventory.AverageCost ?? 0m)) - oldTotalCost) / qtyAfterReversal : 0;
//                                        inventory.StockQuantity -= oldUnits;
//                                    }
//                                }
//                            }

//                            _context.Purchasedetails.RemoveRange(existingPurchase.Purchasedetails);
//                            await _context.SaveChangesAsync();

//                            decimal calculatedTotal = purchase.Purchasedetails.Sum(i => i.Quantity * i.CostPrice);

//                            existingPurchase.InvoiceNumber = purchase.InvoiceNumber;
//                            existingPurchase.SupplierId = purchase.SupplierId;
//                            existingPurchase.Discount = purchase.Discount;
//                            existingPurchase.TaxAmount = purchase.TaxAmount;
//                            existingPurchase.Notes = purchase.Notes;
//                            existingPurchase.TotalAmount = calculatedTotal;
//                            existingPurchase.NetAmount = calculatedNet;
//                            existingPurchase.AmountPaid = purchase.AmountPaid;
//                            existingPurchase.RemainingAmount = purchase.RemainingAmount;
//                            existingPurchase.PaymentStatus = purchase.RemainingAmount <= 0.01m ? "Paid" : (purchase.AmountPaid > 0 ? "Partial" : "Unpaid");
//                            existingPurchase.UpdatedAt = DateTime.Now;
//                            existingPurchase.UpdatedBy = await GetValidUserIdAsync();

//                            // 🚀 الحل: تنظيف البيانات للأسطر المضافة الجديدة في وضع التعديل أيضاً
//                            foreach (var newItem in purchase.Purchasedetails)
//                            {
//                                if (string.IsNullOrWhiteSpace(newItem.BatchNumber))
//                                {
//                                    newItem.BatchNumber = "N/A";
//                                }

//                                newItem.PurchaseId = id;
//                                newItem.SubTotal = newItem.Quantity * newItem.CostPrice;
//                                newItem.RemainingQuantity = newItem.Quantity + newItem.BonusQuantity;
//                                _context.Purchasedetails.Add(newItem);

//                                var drug = await _context.Drugs.FindAsync(newItem.DrugId);
//                                if (drug != null)
//                                {
//                                    int newTotalUnits = (newItem.Quantity + newItem.BonusQuantity) * drug.ConversionFactor;
//                                    decimal newTotalCost = (newItem.Quantity + newItem.BonusQuantity) * newItem.CostPrice;

//                                    var inventory = await _context.Branchinventory.FirstOrDefaultAsync(b => b.DrugId == newItem.DrugId && b.BranchId == existingPurchase.BranchId);
//                                    if (inventory != null)
//                                    {
//                                        decimal currentQty = inventory.StockQuantity > 0 ? inventory.StockQuantity : 0;
//                                        decimal currentCost = inventory.AverageCost ?? 0m;
//                                        decimal combinedQty = currentQty + newTotalUnits;
//                                        if (combinedQty > 0) inventory.AverageCost = ((currentQty * currentCost) + newTotalCost) / combinedQty;
//                                        inventory.StockQuantity += newTotalUnits;
//                                        inventory.CurrentSellingPrice = newItem.SellingPrice;
//                                    }
//                                    else
//                                    {
//                                        _context.Branchinventory.Add(new Branchinventory { BranchId = existingPurchase.BranchId, DrugId = newItem.DrugId, StockQuantity = newTotalUnits, MinimumStockLevel = 10, AverageCost = newTotalUnits > 0 ? (newTotalCost / newTotalUnits) : 0, CurrentSellingPrice = newItem.SellingPrice });
//                                    }

//                                    string generatedCode = $"{newItem.DrugId}-{newItem.BatchNumber}-{newItem.ExpiryDate:yyMM}-{(int)newItem.SellingPrice}";
//                                    _context.BarcodeGenerator.Add(new BarcodeGenerator { BranchId = currentBranchId, DrugId = newItem.DrugId, BatchNumber = newItem.BatchNumber, ExpiryDate = newItem.ExpiryDate, CurrentPrice = newItem.SellingPrice, QuantityToPrint = newTotalUnits, GeneratedCode = generatedCode, IsPrinted = false, CreatedAt = DateTime.Now, UserId = existingPurchase.UpdatedBy.Value });
//                                }
//                            }

//                            _context.Update(existingPurchase);
//                            await _context.SaveChangesAsync();
//                            await transaction.CommitAsync();
//                        }
//                        catch (Exception) { await transaction.RollbackAsync(); throw; }
//                    });

//                    await RecordLog("Edit", "Purchases", $"تعديل فاتورة مشتريات #{purchase.InvoiceNumber}");
//                    TempData["Success"] = "تم تحديث الفاتورة والمخزون بنجاح.";
//                    return RedirectToAction(nameof(Index));
//                }
//                catch (Exception ex) { ViewBag.Error = ex.Message; }
//            }
//            return ReloadEditView(purchase);
//        }

//        private IActionResult ReloadEditView(Purchases purchase)
//        {
//            PrepareDropdowns(purchase.SupplierId);
//            if (purchase.Purchasedetails != null && purchase.Purchasedetails.Any())
//                ViewBag.ExistingDetailsJson = System.Text.Json.JsonSerializer.Serialize(purchase.Purchasedetails.Select(d => new { drugId = d.DrugId, quantity = d.Quantity, bonusQuantity = d.BonusQuantity, costPrice = d.CostPrice, sellingPrice = d.SellingPrice, batchNumber = d.BatchNumber, expiryDate = d.ExpiryDate.ToString("yyyy-MM-dd"), subTotal = d.SubTotal }).ToList());
//            else
//                ViewBag.ExistingDetailsJson = "[]";
//            return View(purchase);
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        [HasPermission("Purchases", "Delete")]
//        public async Task<IActionResult> Delete(int id)
//        {
//            int currentBranchId = ActiveBranchId;
//            var purchase = await _context.Purchases.FirstOrDefaultAsync(p => p.PurchaseId == id && p.BranchId == currentBranchId);
//            if (purchase != null)
//            {
//                _context.Purchases.Remove(purchase);
//                await _context.SaveChangesAsync();
//                await RecordLog("Delete", "Purchases", $"إلغاء فاتورة مشتريات #{purchase.InvoiceNumber}");
//                TempData["Success"] = "تم إلغاء الفاتورة بنجاح.";
//            }
//            return RedirectToAction(nameof(Index));
//        }

//        private async Task<decimal> CalculateAccountBalance(int accountId)
//        {
//            var account = await _context.Accounts.FindAsync(accountId);
//            if (account == null) return 0m;

//            decimal debit = await _context.Journaldetails.Where(d => d.AccountId == accountId && d.Journal.IsPosted).SumAsync(d => (decimal?)d.Debit) ?? 0m;
//            decimal credit = await _context.Journaldetails.Where(d => d.AccountId == accountId && d.Journal.IsPosted).SumAsync(d => (decimal?)d.Credit) ?? 0m;

//            return account.AccountNature ? (debit - credit) : (credit - debit);
//        }

//        [HttpGet]
//        [HasPermission("Purchases", "View")]
//        public async Task<IActionResult> GetAccountBalance(int accountId)
//        {
//            decimal balance = await CalculateAccountBalance(accountId);
//            return Json(new { success = true, balance });
//        }

//        private void PrepareDropdowns(int? selectedSupplierId = null)
//        {
//            int currentBranchId = ActiveBranchId;
//            var suppliersList = _context.Suppliers.Where(s => s.IsActive == true && (s.BranchId == currentBranchId || s.BranchId == null || s.BranchId == 1)).ToList();
//            ViewBag.Suppliers = new SelectList(suppliersList, "SupplierId", "SupplierName", selectedSupplierId);

//            var drugsList = _context.Drugs.Where(d => d.IsActive == true).Select(d => new { id = d.DrugId, name = d.DrugName, barcode = d.Barcode, unit = d.MainUnit }).ToList();
//            ViewBag.DrugsJson = System.Text.Json.JsonSerializer.Serialize(drugsList);

//            ViewBag.CashAccounts = _context.Accounts.Where(a => a.IsActive == true && !a.SubAccounts.Any() && (a.AccountName.Contains("صندوق") || a.AccountName.Contains("نقد")) && (a.BranchId == currentBranchId || a.BranchId == null)).ToList();
//            ViewBag.BankAccounts = _context.Accounts.Where(a => a.IsActive == true && !a.SubAccounts.Any() && (a.AccountName.Contains("بنك") || a.AccountName.Contains("مصرف") || a.AccountName.Contains("حساب")) && (a.BranchId == currentBranchId || a.BranchId == null)).ToList();
//        }

//        [HttpGet]
//        [HasPermission("Purchases", "View")]
//        public async Task<IActionResult> GetDrugsList()
//        {
//            var drugsList = await _context.Drugs.Where(d => d.IsActive == true).Select(d => new { id = d.DrugId, name = d.DrugName, barcode = d.Barcode, unit = d.MainUnit }).ToListAsync();
//            return Json(drugsList);
//        }
//    }
//}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Models;
using PharmaSmartWeb.Filters;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using PharmaSmartWeb.Services;

namespace PharmaSmartWeb.Controllers
{
    [Authorize]
    public class PurchasesController : BaseController
    {
        private readonly IAccountingEngine _accountingEngine;

        public PurchasesController(ApplicationDbContext context, IAccountingEngine accountingEngine) : base(context)
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
            var fallbackUser = await _context.Users.FirstOrDefaultAsync();
            if (fallbackUser == null) throw new Exception("لا يوجد مستخدم مسجل لربط العملية به!");
            return fallbackUser.UserId;
        }

        [HttpGet]
        [HasPermission("Purchases", "View")]
        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 50;
            int currentBranchId = ActiveBranchId;
            
            // 🚀 الإحصائيات الشاملة (بدون GroupBy(1) لتفادي أخطاء EF Core 3.1)
            var statsQuery = _context.Purchases.Where(p => p.BranchId == currentBranchId);

            decimal totalAmountAll = await statsQuery.SumAsync(x => x.TotalAmount);
            decimal totalPaid = await statsQuery.Where(x => x.PaymentStatus == "Paid").SumAsync(x => x.NetAmount);
            decimal totalUnpaid = await statsQuery.Where(x => x.PaymentStatus != "Paid").SumAsync(x => x.NetAmount);
            int unpaidCount = await statsQuery.CountAsync(x => x.PaymentStatus != "Paid");
            int totalPurchasesCount = await statsQuery.CountAsync();

            ViewBag.TotalAmountAll = totalAmountAll;
            ViewBag.TotalPaid = totalPaid;
            ViewBag.TotalUnpaid = totalUnpaid;
            ViewBag.UnpaidCount = unpaidCount;
            ViewBag.TotalPurchasesCount = totalPurchasesCount;

            // 🚀 نظام التقسيم الذكي (Server-Side Pagination) ومنع تسريب الذاكرة بـ AsNoTracking
            var baseQuery = _context.Purchases
                .AsNoTracking()
                .Include(p => p.Supplier)
                .Where(p => p.BranchId == currentBranchId);

            int totalRecords = await baseQuery.CountAsync();
            var purchases = await baseQuery
                .OrderByDescending(p => p.PurchaseDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            return View(purchases);
        }

        [HttpGet]
        [HasPermission("Purchases", "View")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            int currentBranchId = ActiveBranchId;

            var purchase = await _context.Purchases
                .Include(p => p.Supplier).Include(p => p.User)
                .Include(p => p.Purchasedetails).ThenInclude(d => d.Drug)
                .FirstOrDefaultAsync(m => m.PurchaseId == id && m.BranchId == currentBranchId);

            if (purchase == null) return NotFound();
            return View(purchase);
        }

        [HttpGet]
        [HasPermission("Purchases", "Add")]
        public IActionResult Create()
        {
            PrepareDropdowns();
            return View(new Purchases { PurchaseDate = DateTime.Now, AmountPaid = 0 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Purchases", "Add")]
        public async Task<IActionResult> Create(Purchases purchase, decimal CashAmount, int? CashAccountId, decimal BankAmount, int? BankAccountId)
        {
            int currentBranchId = ActiveBranchId;
            ModelState.Remove("Supplier"); ModelState.Remove("User"); ModelState.Remove("Branch"); ModelState.Remove("PaymentStatus");

            if (purchase.Purchasedetails != null)
            {
                var detailsList = purchase.Purchasedetails.ToList();
                for (int i = 0; i < detailsList.Count; i++) { ModelState.Remove($"Purchasedetails[{i}].Purchase"); ModelState.Remove($"Purchasedetails[{i}].Drug"); }
            }

            if (!ModelState.IsValid)
            {
                var errors = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                ViewBag.Error = "رفض النظام بسبب أخطاء في البيانات المكتوبة: " + errors;
                return ReloadCreateView(purchase);
            }

            if (purchase.Purchasedetails == null || !purchase.Purchasedetails.Any())
            {
                ViewBag.Error = "يجب إضافة صنف واحد على الأقل للفاتورة.";
                return ReloadCreateView(purchase);
            }

            // 🛡️ حاجز أمني: التحقق من عدم تكرار رقم الفاتورة لنفس المورد في نفس الفرع
            bool isDuplicateInvoice = await _context.Purchases.AnyAsync(p =>
                p.SupplierId == purchase.SupplierId &&
                p.InvoiceNumber == purchase.InvoiceNumber &&
                p.BranchId == currentBranchId);

            if (isDuplicateInvoice)
            {
                var supplierCheckDup = await _context.Suppliers.FindAsync(purchase.SupplierId);
                ViewBag.Error = $"عفواً، رقم الفاتورة ({purchase.InvoiceNumber}) مسجل مسبقاً للمورد ({supplierCheckDup?.SupplierName}). يرجى التأكد من الرقم الخارجي للفاتورة لمنع التكرار.";
                return ReloadCreateView(purchase);
            }

            var supplierCheck = await _context.Suppliers.FindAsync(purchase.SupplierId);
            if (supplierCheck == null || supplierCheck.AccountId == null || supplierCheck.AccountId == 0)
            {
                ViewBag.Error = $"المورد المختار ({supplierCheck?.SupplierName}) غير مربوط بحساب مالي في الدليل المحاسبي. يرجى الدخول لشاشة الموردين وربطه بحساب.";
                return ReloadCreateView(purchase);
            }

            if (CashAmount > 0 && (CashAccountId == null || CashAccountId <= 0))
            {
                ViewBag.Error = "لقد قمت بإدخال مبلغ نقدي، يرجى اختيار الصندوق المراد السحب منه.";
                return ReloadCreateView(purchase);
            }

            if (BankAmount > 0 && (BankAccountId == null || BankAccountId <= 0))
            {
                ViewBag.Error = "لقد قمت بإدخال مبلغ حوالة، يرجى اختيار البنك المراد السحب منه.";
                return ReloadCreateView(purchase);
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        decimal calculatedTotal = purchase.Purchasedetails.Sum(i => i.Quantity * i.CostPrice);
                        decimal calculatedNet = calculatedTotal - purchase.Discount + purchase.TaxAmount;

                        purchase.AmountPaid = CashAmount + BankAmount;
                        if (purchase.AmountPaid > calculatedNet) purchase.AmountPaid = calculatedNet;

                        purchase.RemainingAmount = calculatedNet - purchase.AmountPaid;

                        purchase.UserId = await GetValidUserIdAsync();
                        purchase.BranchId = currentBranchId;
                        purchase.PurchaseDate = DateTime.Now;
                        purchase.PaymentStatus = purchase.RemainingAmount <= 0.01m ? "Paid" : (purchase.AmountPaid > 0 ? "Partial" : "Unpaid");
                        purchase.TotalAmount = calculatedTotal;
                        purchase.NetAmount = calculatedNet;
                        purchase.CreatedAt = DateTime.Now;

                        foreach (var item in purchase.Purchasedetails)
                        {
                            if (string.IsNullOrWhiteSpace(item.BatchNumber))
                            {
                                item.BatchNumber = "N/A";
                            }
                            item.SubTotal = item.Quantity * item.CostPrice;
                            item.RemainingQuantity = item.Quantity + item.BonusQuantity;
                        }

                        _context.Purchases.Add(purchase);
                        await _context.SaveChangesAsync();

                        foreach (var item in purchase.Purchasedetails)
                        {
                            var drug = await _context.Drugs.FindAsync(item.DrugId);
                            if (drug != null)
                            {
                                int totalUnitsToAdd = (item.Quantity + item.BonusQuantity) * drug.ConversionFactor;
                                decimal totalCostPaid = item.Quantity * item.CostPrice;

                                if (item.BatchNumber != "N/A")
                                {
                                    var existingBatch = await _context.DrugBatches.FirstOrDefaultAsync(b => b.DrugId == item.DrugId && b.BatchNumber == item.BatchNumber);
                                    if (existingBatch == null) _context.DrugBatches.Add(new DrugBatches { DrugId = item.DrugId, BatchNumber = item.BatchNumber, ExpiryDate = item.ExpiryDate });
                                }

                                var inventory = await _context.Branchinventory.FirstOrDefaultAsync(b => b.DrugId == item.DrugId && b.BranchId == currentBranchId);
                                if (inventory != null)
                                {
                                    decimal currentQty = inventory.StockQuantity > 0 ? inventory.StockQuantity : 0;
                                    decimal currentCost = inventory.AverageCost ?? 0m;
                                    decimal newTotalQty = currentQty + totalUnitsToAdd;

                                    if (newTotalQty > 0) inventory.AverageCost = ((currentQty * currentCost) + totalCostPaid) / newTotalQty;

                                    inventory.StockQuantity += totalUnitsToAdd;
                                    inventory.CurrentSellingPrice = item.SellingPrice;
                                    _context.Branchinventory.Update(inventory);
                                }
                                else
                                {
                                    decimal newUnitCost = totalUnitsToAdd > 0 ? (totalCostPaid / totalUnitsToAdd) : 0;
                                    inventory = new Branchinventory { DrugId = item.DrugId, BranchId = currentBranchId, StockQuantity = totalUnitsToAdd, MinimumStockLevel = 10, AverageCost = newUnitCost, CurrentSellingPrice = item.SellingPrice };
                                    _context.Branchinventory.Add(inventory);
                                }

                                _context.Stockmovements.Add(new Stockmovements { DrugId = item.DrugId, BranchId = currentBranchId, MovementType = "Purchase In", Quantity = totalUnitsToAdd, MovementDate = DateTime.Now, ReferenceId = purchase.PurchaseId, UserId = purchase.UserId, Notes = $"توريد - فاتورة {purchase.InvoiceNumber}" });

                                string generatedCode = $"{item.DrugId}-{item.BatchNumber}-{item.ExpiryDate:yyMM}-{(int)item.SellingPrice}";
                                _context.BarcodeGenerator.Add(new BarcodeGenerator { BranchId = currentBranchId, DrugId = item.DrugId, BatchNumber = item.BatchNumber, ExpiryDate = item.ExpiryDate, CurrentPrice = item.SellingPrice, QuantityToPrint = totalUnitsToAdd, GeneratedCode = generatedCode, IsPrinted = false, CreatedAt = DateTime.Now, UserId = purchase.UserId });
                            }
                        }

                        await _context.SaveChangesAsync();

                        // 🚀 التصحيح المحاسبي للبونص: نمرر الصافي الفعلي المدفوع دون تضمين قيمة البونص الوهمية 
                        var payload = new AccountingPayload
                        {
                            TransactionType = TransactionType.PurchaseInvoice,
                            BranchId = currentBranchId,
                            UserId = purchase.UserId,
                            ReferenceNo = purchase.InvoiceNumber,
                            Description = $"فاتورة مشتريات #{purchase.InvoiceNumber} - المورد: {supplierCheck.SupplierName}",
                            SupplierId = purchase.SupplierId,
                            SpecificCashAccountId = CashAmount > 0 ? CashAccountId : null,
                            SpecificBankAccountId = BankAmount > 0 ? BankAccountId : null
                        };

                        // القيمة الدقيقة للمخزون هي المبلغ الصافي الفعلي فقط (calculatedNet)
                        payload.Amounts.Add(AmountSource.NetTotalAmount, calculatedNet);
                        payload.Amounts.Add(AmountSource.PaidCashAmount, CashAmount);
                        payload.Amounts.Add(AmountSource.PaidBankAmount, BankAmount);
                        payload.Amounts.Add(AmountSource.CreditAmount, purchase.RemainingAmount);

                        await _accountingEngine.ProcessTransactionAsync(payload);

                        await transaction.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        throw new Exception(ex.Message + (ex.InnerException != null ? " - " + ex.InnerException.Message : ""));
                    }
                });

                await RecordLog("Add", "Purchases", $"فاتورة توريد #{purchase.InvoiceNumber} (مدفوع: {purchase.AmountPaid})");
                TempData["Success"] = "تم حفظ الفاتورة وتحديث المخزون وتوليد القيد المحاسبي بنجاح.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Error = "النظام رفض الحفظ للسبب التالي: " + ex.Message;
                return ReloadCreateView(purchase);
            }
        }

        private IActionResult ReloadCreateView(Purchases purchase)
        {
            PrepareDropdowns(purchase.SupplierId);

            if (purchase.Purchasedetails != null && purchase.Purchasedetails.Any())
            {
                var recoveredCart = new List<object>();
                foreach (var item in purchase.Purchasedetails)
                {
                    var drugName = _context.Drugs.Where(d => d.DrugId == item.DrugId).Select(d => d.DrugName).FirstOrDefault() ?? "صنف غير معروف";
                    recoveredCart.Add(new
                    {
                        uid = DateTime.Now.Ticks + item.DrugId,
                        id = item.DrugId,
                        name = drugName,
                        qty = item.Quantity,
                        bonus = item.BonusQuantity,
                        cost = item.CostPrice,
                        sell = item.SellingPrice,
                        batch = item.BatchNumber ?? "",
                        expiry = item.ExpiryDate.ToString("yyyy-MM-dd")
                    });
                }
                ViewBag.RecoveredCartJson = System.Text.Json.JsonSerializer.Serialize(recoveredCart);
            }

            return View(purchase);
        }

        [HttpGet]
        [HasPermission("Purchases", "Edit")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            int currentBranchId = ActiveBranchId;

            var purchase = await _context.Purchases.Include(p => p.Purchasedetails).FirstOrDefaultAsync(p => p.PurchaseId == id && p.BranchId == currentBranchId);
            if (purchase == null) return NotFound();

            // 🛡️ الحاجز الأمني (Anti-Tamper Logic)
            if (purchase.PurchaseDate < DateTime.Now.AddDays(-7))
            {
                TempData["Error"] = "حماية النظام (Anti-Tamper): لا يمكن تعديل فاتورة مرت عليها فترة السماح (7 أيام). يرجى إجراء فاتورة إرجاع أو تسوية بنكية بدلاً من التعديل المباشر.";
                return RedirectToAction(nameof(Index));
            }

            return ReloadEditView(purchase);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Purchases", "Edit")]
        public async Task<IActionResult> Edit(int id, Purchases purchase, int? PaidFromAccountId)
        {
            if (id != purchase.PurchaseId) return NotFound();
            int currentBranchId = ActiveBranchId;

            // 🛡️ الحاجز الأمني (Anti-Tamper Logic)
            var securityCheckPurchase = await _context.Purchases.AsNoTracking().FirstOrDefaultAsync(p => p.PurchaseId == id);
            if (securityCheckPurchase != null && securityCheckPurchase.PurchaseDate < DateTime.Now.AddDays(-7))
            {
                TempData["Error"] = "حماية النظام (Anti-Tamper): لا يمكن تعديل فاتورة مرت عليها فترة السماح (7 أيام). يرجى إجراء فاتورة إرجاع أو تسوية بنكية بدلاً من التعديل المباشر.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.Remove("Supplier"); ModelState.Remove("User"); ModelState.Remove("Branch"); ModelState.Remove("PaymentStatus");

            if (purchase.Purchasedetails != null)
            {
                var detailsList = purchase.Purchasedetails.ToList();
                for (int i = 0; i < detailsList.Count; i++) { ModelState.Remove($"Purchasedetails[{i}].Purchase"); ModelState.Remove($"Purchasedetails[{i}].Drug"); }
            }

            if (ModelState.IsValid)
            {
                decimal calculatedNet = purchase.Purchasedetails.Sum(i => i.Quantity * i.CostPrice) - purchase.Discount + purchase.TaxAmount;

                if (purchase.PaymentStatus == "Paid") purchase.AmountPaid = calculatedNet;
                else purchase.AmountPaid = 0;

                purchase.RemainingAmount = calculatedNet - purchase.AmountPaid;

                if (purchase.AmountPaid > 0 && PaidFromAccountId is int paidAccId && paidAccId > 0)
                {
                    decimal actualBalance = await CalculateAccountBalance(paidAccId);
                    var oldPurchase = await _context.Purchases.AsNoTracking().FirstOrDefaultAsync(p => p.PurchaseId == id);
                    if (oldPurchase != null) actualBalance += oldPurchase.AmountPaid;

                    if (actualBalance < purchase.AmountPaid)
                    {
                        ViewBag.Error = $"عفواً، الرصيد المتاح لتعديل الفاتورة لا يكفي.";
                        return ReloadEditView(purchase);
                    }
                }

                var strategy = _context.Database.CreateExecutionStrategy();
                try
                {
                    await strategy.ExecuteAsync(async () =>
                    {
                        using var transaction = await _context.Database.BeginTransactionAsync();
                        try
                        {
                            var existingPurchase = await _context.Purchases.Include(p => p.Purchasedetails).FirstOrDefaultAsync(p => p.PurchaseId == id && p.BranchId == currentBranchId);
                            if (existingPurchase == null) throw new Exception("الفاتورة غير موجودة.");

                            foreach (var oldItem in existingPurchase.Purchasedetails)
                            {
                                var drug = await _context.Drugs.FindAsync(oldItem.DrugId);
                                if (drug != null)
                                {
                                    var inventory = await _context.Branchinventory.FirstOrDefaultAsync(b => b.DrugId == oldItem.DrugId && b.BranchId == existingPurchase.BranchId);
                                    if (inventory != null)
                                    {
                                        int oldUnits = (oldItem.Quantity + oldItem.BonusQuantity) * drug.ConversionFactor;
                                        decimal oldTotalCost = (oldItem.Quantity + oldItem.BonusQuantity) * oldItem.CostPrice;
                                        decimal qtyAfterReversal = inventory.StockQuantity - oldUnits;
                                        inventory.AverageCost = qtyAfterReversal > 0 ? ((inventory.StockQuantity * (inventory.AverageCost ?? 0m)) - oldTotalCost) / qtyAfterReversal : 0;
                                        inventory.StockQuantity -= oldUnits;
                                    }
                                }
                            }

                            _context.Purchasedetails.RemoveRange(existingPurchase.Purchasedetails);
                            await _context.SaveChangesAsync();

                            decimal calculatedTotal = purchase.Purchasedetails.Sum(i => i.Quantity * i.CostPrice);

                            existingPurchase.InvoiceNumber = purchase.InvoiceNumber;
                            existingPurchase.SupplierId = purchase.SupplierId;
                            existingPurchase.Discount = purchase.Discount;
                            existingPurchase.TaxAmount = purchase.TaxAmount;
                            existingPurchase.Notes = purchase.Notes;
                            existingPurchase.TotalAmount = calculatedTotal;
                            existingPurchase.NetAmount = calculatedNet;
                            existingPurchase.AmountPaid = purchase.AmountPaid;
                            existingPurchase.RemainingAmount = purchase.RemainingAmount;
                            existingPurchase.PaymentStatus = purchase.RemainingAmount <= 0.01m ? "Paid" : (purchase.AmountPaid > 0 ? "Partial" : "Unpaid");
                            existingPurchase.UpdatedAt = DateTime.Now;
                            existingPurchase.UpdatedBy = await GetValidUserIdAsync();

                            foreach (var newItem in purchase.Purchasedetails)
                            {
                                if (string.IsNullOrWhiteSpace(newItem.BatchNumber))
                                {
                                    newItem.BatchNumber = "N/A";
                                }

                                newItem.PurchaseId = id;
                                newItem.SubTotal = newItem.Quantity * newItem.CostPrice;
                                newItem.RemainingQuantity = newItem.Quantity + newItem.BonusQuantity;
                                _context.Purchasedetails.Add(newItem);

                                var drug = await _context.Drugs.FindAsync(newItem.DrugId);
                                if (drug != null)
                                {
                                    int newTotalUnits = (newItem.Quantity + newItem.BonusQuantity) * drug.ConversionFactor;
                                    decimal newTotalCost = (newItem.Quantity + newItem.BonusQuantity) * newItem.CostPrice;

                                    var inventory = await _context.Branchinventory.FirstOrDefaultAsync(b => b.DrugId == newItem.DrugId && b.BranchId == existingPurchase.BranchId);
                                    if (inventory != null)
                                    {
                                        decimal currentQty = inventory.StockQuantity > 0 ? inventory.StockQuantity : 0;
                                        decimal currentCost = inventory.AverageCost ?? 0m;
                                        decimal combinedQty = currentQty + newTotalUnits;
                                        if (combinedQty > 0) inventory.AverageCost = ((currentQty * currentCost) + newTotalCost) / combinedQty;
                                        inventory.StockQuantity += newTotalUnits;
                                        inventory.CurrentSellingPrice = newItem.SellingPrice;
                                    }
                                    else
                                    {
                                        _context.Branchinventory.Add(new Branchinventory { BranchId = existingPurchase.BranchId, DrugId = newItem.DrugId, StockQuantity = newTotalUnits, MinimumStockLevel = 10, AverageCost = newTotalUnits > 0 ? (newTotalCost / newTotalUnits) : 0, CurrentSellingPrice = newItem.SellingPrice });
                                    }

                                    string generatedCode = $"{newItem.DrugId}-{newItem.BatchNumber}-{newItem.ExpiryDate:yyMM}-{(int)newItem.SellingPrice}";
                                    _context.BarcodeGenerator.Add(new BarcodeGenerator { BranchId = currentBranchId, DrugId = newItem.DrugId, BatchNumber = newItem.BatchNumber, ExpiryDate = newItem.ExpiryDate, CurrentPrice = newItem.SellingPrice, QuantityToPrint = newTotalUnits, GeneratedCode = generatedCode, IsPrinted = false, CreatedAt = DateTime.Now, UserId = existingPurchase.UpdatedBy.Value });
                                }
                            }

                            _context.Update(existingPurchase);
                            await _context.SaveChangesAsync();
                            await transaction.CommitAsync();
                        }
                        catch (Exception) { await transaction.RollbackAsync(); throw; }
                    });

                    await RecordLog("Edit", "Purchases", $"تعديل فاتورة مشتريات #{purchase.InvoiceNumber}");
                    TempData["Success"] = "تم تحديث الفاتورة والمخزون بنجاح.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex) { ViewBag.Error = ex.Message; }
            }
            return ReloadEditView(purchase);
        }

        private IActionResult ReloadEditView(Purchases purchase)
        {
            PrepareDropdowns(purchase.SupplierId);
            if (purchase.Purchasedetails != null && purchase.Purchasedetails.Any())
                ViewBag.ExistingDetailsJson = System.Text.Json.JsonSerializer.Serialize(purchase.Purchasedetails.Select(d => new { drugId = d.DrugId, quantity = d.Quantity, bonusQuantity = d.BonusQuantity, costPrice = d.CostPrice, sellingPrice = d.SellingPrice, batchNumber = d.BatchNumber, expiryDate = d.ExpiryDate.ToString("yyyy-MM-dd"), subTotal = d.SubTotal }).ToList());
            else
                ViewBag.ExistingDetailsJson = "[]";
            return View(purchase);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Purchases", "Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            int currentBranchId = ActiveBranchId;
            var purchase = await _context.Purchases.FirstOrDefaultAsync(p => p.PurchaseId == id && p.BranchId == currentBranchId);
            
            // 🛡️ الحاجز الأمني (Anti-Tamper Logic)
            if (purchase != null && purchase.PurchaseDate < DateTime.Now.AddDays(-7))
            {
                TempData["Error"] = "حماية النظام (Anti-Tamper): لا يمكن حذف فاتورة مرت عليها فترة السماح (7 أيام). يرجى إجراء دورة عكسية أو تسوية بنكية بدلاً من الحذف.";
                return RedirectToAction(nameof(Index));
            }

            if (purchase != null)
            {
                _context.Purchases.Remove(purchase);
                await _context.SaveChangesAsync();
                await RecordLog("Delete", "Purchases", $"إلغاء فاتورة مشتريات #{purchase.InvoiceNumber}");
                TempData["Success"] = "تم إلغاء الفاتورة بنجاح.";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<decimal> CalculateAccountBalance(int accountId)
        {
            var account = await _context.Accounts.FindAsync(accountId);
            if (account == null) return 0m;

            decimal debit = await _context.Journaldetails.Where(d => d.AccountId == accountId && d.Journal.IsPosted).SumAsync(d => (decimal?)d.Debit) ?? 0m;
            decimal credit = await _context.Journaldetails.Where(d => d.AccountId == accountId && d.Journal.IsPosted).SumAsync(d => (decimal?)d.Credit) ?? 0m;

            return account.AccountNature ? (debit - credit) : (credit - debit);
        }

        [HttpGet]
        [HasPermission("Purchases", "View")]
        public async Task<IActionResult> GetAccountBalance(int accountId)
        {
            decimal balance = await CalculateAccountBalance(accountId);
            return Json(new { success = true, balance });
        }

        private void PrepareDropdowns(int? selectedSupplierId = null)
        {
            int currentBranchId = ActiveBranchId;
            var suppliersList = _context.Suppliers.Where(s => s.IsActive == true && (s.BranchId == currentBranchId || s.BranchId == 1)).ToList();
            ViewBag.Suppliers = new SelectList(suppliersList, "SupplierId", "SupplierName", selectedSupplierId);

            var drugsList = _context.Drugs.Where(d => d.IsActive == true).Select(d => new { id = d.DrugId, name = d.DrugName, barcode = d.Barcode, unit = d.MainUnit }).ToList();
            ViewBag.DrugsJson = System.Text.Json.JsonSerializer.Serialize(drugsList);

            ViewBag.CashAccounts = _context.Accounts.Where(a => a.IsActive == true && a.IsParent == false && (a.AccountName.Contains("صندوق") || a.AccountName.Contains("نقد")) && (a.BranchId == currentBranchId || a.BranchId == null)).ToList();
            ViewBag.BankAccounts = _context.Accounts.Where(a => a.IsActive == true && a.IsParent == false && (a.AccountName.Contains("بنك") || a.AccountName.Contains("مصرف") || a.AccountName.Contains("حساب")) && (a.BranchId == currentBranchId || a.BranchId == null)).ToList();
        }

        [HttpGet]
        [HasPermission("Purchases", "View")]
        public async Task<IActionResult> GetDrugsList()
        {
            var drugsList = await _context.Drugs.Where(d => d.IsActive == true).Select(d => new { id = d.DrugId, name = d.DrugName, barcode = d.Barcode, unit = d.MainUnit }).ToListAsync();
            return Json(drugsList);
        }
    }
}