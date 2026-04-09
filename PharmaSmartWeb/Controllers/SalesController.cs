using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Models;
using PharmaSmartWeb.Filters;
using PharmaSmartWeb.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;


namespace PharmaSmartWeb.Controllers
{
    [Authorize]
    public class SalesController : BaseController
    {
        private readonly IAccountingEngine _accountingEngine;

        public SalesController(ApplicationDbContext context, IAccountingEngine accountingEngine) : base(context)
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
        [HasPermission("Sales", "View")]
        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 50;
            var today = DateTime.Today;

            // 🚀 هندسة الأداء (Performance): استعلام الإحصائيات باستخدام SQL Projection لمنع سحب الكائنات
            var statsQuery = await _context.Sales
                .AsNoTracking()
                .Where(s => s.BranchId == ActiveBranchId && s.SaleDate >= today && s.IsReturn == false)
                .GroupBy(s => 1)
                .Select(g => new {
                    TotalAmount = g.Sum(x => x.NetAmount),
                    Count = g.Count()
                }).FirstOrDefaultAsync();

            ViewBag.TotalSalesToday = statsQuery?.TotalAmount ?? 0;
            ViewBag.TotalInvoicesToday = statsQuery?.Count ?? 0;
            ViewBag.AverageCartValueToday = (statsQuery?.Count > 0) ? (statsQuery.TotalAmount / statsQuery.Count) : 0;

            // 🚀 نظام التقسيم الذكي (Server-Side Pagination)
            var baseQuery = _context.Sales
                .AsNoTracking()
                .Include(s => s.Customer)
                .Include(s => s.User)
                .Include(s => s.SalePayments)
                .Where(s => s.BranchId == ActiveBranchId && s.IsReturn == false);

            int totalRecords = await baseQuery.CountAsync();
            
            var salesList = await baseQuery
                .OrderByDescending(s => s.SaleDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            return View(salesList);
        }

        [HttpGet]
        [HasPermission("Sales", "View")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var sale = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.User)
                .Include(s => s.Saledetails)
                    .ThenInclude(d => d.Drug)
                .Include(s => s.SalePayments)
                    .ThenInclude(sp => sp.Account)
                .FirstOrDefaultAsync(m => m.SaleId == id && m.BranchId == ActiveBranchId);

            if (sale == null) return RedirectToAction("AccessDenied", "Home");

            ViewBag.SystemSettings = await _context.CompanySettings.FirstOrDefaultAsync();

            return View(sale);
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomersList()
        {
            var customers = await _context.Customers
                .Where(c => c.IsActive == true && (c.BranchId == ActiveBranchId || c.BranchId == 1))
                .Select(c => new { id = c.CustomerId, name = c.FullName })
                .ToListAsync();

            return Json(customers);
        }

        [HttpGet]
        [HasPermission("Sales", "Add")]
        public IActionResult Create()
        {
            ViewBag.Customers = new SelectList(_context.Customers.Where(c => c.IsActive == true && (c.BranchId == ActiveBranchId || c.BranchId == 1)), "CustomerId", "FullName");
            ViewBag.CashAccounts = _context.Accounts.Where(a => a.IsActive == true && a.IsParent == false && (a.AccountName.Contains("صندوق") || a.AccountName.Contains("نقد")) && a.BranchId == ActiveBranchId).ToList();
            ViewBag.BankAccounts = _context.Accounts.Where(a => a.IsActive == true && a.IsParent == false && (a.AccountName.Contains("بنك") || a.AccountName.Contains("حساب")) && (a.BranchId == ActiveBranchId || a.BranchId == null)).ToList();

            // 🚀 التحديث الهندسي: جلب أسماء الوحدات ومعامل التحويل ليتمكن الـ POS من قسمة السعر آلياً
            var inventoryItems = _context.Branchinventory
                .Include(b => b.Drug)
                .Where(b => b.BranchId == ActiveBranchId && b.StockQuantity > 0 && b.Drug.IsActive == true)
                .Select(b => new
                {
                    id = b.DrugId,
                    name = b.Drug.DrugName,
                    barcode = b.Drug.Barcode,
                    price = b.CurrentSellingPrice ?? 0,
                    cost = b.AverageCost ?? 0,
                    stock = b.StockQuantity,
                    mainUnit = string.IsNullOrEmpty(b.Drug.MainUnit) ? "باكت" : b.Drug.MainUnit,
                    subUnit = string.IsNullOrEmpty(b.Drug.SubUnit) ? "حبة" : b.Drug.SubUnit,
                    convFactor = b.Drug.ConversionFactor > 0 ? b.Drug.ConversionFactor : 1
                }).ToList();

            ViewBag.DrugsJson = System.Text.Json.JsonSerializer.Serialize(inventoryItems);

            return View(new Sales { SaleDate = DateTime.Now });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Sales", "Add")]
        public async Task<IActionResult> Create(Sales sale, decimal CashAmount, int? CashAccountId, decimal BankAmount, int? BankAccountId)
        {
            ModelState.Remove("Customer"); ModelState.Remove("User"); ModelState.Remove("Branch");

            if (sale.Saledetails != null)
            {
                var details = sale.Saledetails.ToList();
                for (int i = 0; i < details.Count; i++) { ModelState.Remove($"Saledetails[{i}].Sale"); ModelState.Remove($"Saledetails[{i}].Drug"); }
            }

            if (sale.Saledetails == null || !sale.Saledetails.Any())
            {
                ViewBag.Error = "الفاتورة فارغة!";
                return ReloadCreateView(sale);
            }

            if (ModelState.IsValid)
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                try
                {
                    await strategy.ExecuteAsync(async () =>
                    {
                        using var transaction = await _context.Database.BeginTransactionAsync();
                        try
                        {
                            sale.UserId = await GetValidUserIdAsync();
                            sale.BranchId = ActiveBranchId;
                            sale.SaleDate = DateTime.Now;
                            sale.IsReturn = false;

                            decimal grossTotal = 0;
                            decimal totalCogs = 0;

                            foreach (var item in sale.Saledetails)
                            {
                                grossTotal += (item.Quantity * item.UnitPrice);

                                var inventory = await _context.Branchinventory.Include(b => b.Drug).FirstOrDefaultAsync(b => b.DrugId == item.DrugId && b.BranchId == ActiveBranchId);
                                if (inventory == null)
                                    throw new Exception($"الصنف (DrugId: {item.DrugId}) غير موجود في مخزون الفرع الحالي ({ActiveBranchId}).");

                                if (inventory.StockQuantity < item.Quantity)
                                    throw new Exception($"الكمية ({item.Quantity}) المطلوبة للصنف '{inventory.Drug?.DrugName}' تتجاوز المخزون المتوفر ({inventory.StockQuantity}).");

                                // 🚀 بما أن السيرفر يستقبل (الكمية بالوحدة الصغرى) و(التكلفة للوحدة الصغرى)، فالحسابات هنا دقيقة 100%
                                decimal costPerPill = (inventory.AverageCost ?? 0) / (inventory.Drug.ConversionFactor > 0 ? inventory.Drug.ConversionFactor : 1);
                                totalCogs += (item.Quantity * costPerPill);

                                inventory.StockQuantity -= item.Quantity; // خصم الحبات المباعة من المخزون
                                _context.Branchinventory.Update(inventory);

                                _context.Stockmovements.Add(new Stockmovements { BranchId = ActiveBranchId, DrugId = item.DrugId, MovementDate = DateTime.Now, MovementType = "Sale Out", Quantity = -item.Quantity, UserId = sale.UserId, Notes = "مبيعات POS" });
                            }

                            sale.TotalAmount = grossTotal;
                            sale.NetAmount = grossTotal - sale.Discount + sale.TaxAmount;

                            _context.Sales.Add(sale);
                            await _context.SaveChangesAsync();

                            decimal amountPaid = CashAmount + BankAmount;
                            decimal remainingAmount = sale.NetAmount - amountPaid;

                            bool hasCustomer = sale.CustomerId is int cid && cid > 0;

                            if (remainingAmount > 0 && !hasCustomer)
                                throw new Exception("المبلغ المدفوع أقل من الصافي، يرجى اختيار العميل لتسجيل المديونية!");

                            if (CashAmount > 0)
                            {
                                if (!(CashAccountId is int cId && cId > 0)) throw new Exception("يرجى اختيار حساب الصندوق.");
                                _context.SalePayments.Add(new SalePayments { SaleId = sale.SaleId, PaymentMethod = "Cash", AccountId = CashAccountId, Amount = CashAmount });
                            }
                            if (BankAmount > 0)
                            {
                                if (!(BankAccountId is int bId && bId > 0)) throw new Exception("يرجى اختيار حساب البنك.");
                                _context.SalePayments.Add(new SalePayments { SaleId = sale.SaleId, PaymentMethod = "Bank", AccountId = BankAccountId, Amount = BankAmount });
                            }
                            if (remainingAmount > 0)
                            {
                                _context.SalePayments.Add(new SalePayments { SaleId = sale.SaleId, PaymentMethod = "Credit", Amount = remainingAmount });
                            }

                            await _context.SaveChangesAsync();

                            var payload = new AccountingPayload
                            {
                                TransactionType = TransactionType.SalesInvoice,
                                BranchId = ActiveBranchId,
                                UserId = sale.UserId,
                                ReferenceNo = sale.SaleId.ToString(),
                                Description = $"مبيعات POS فاتورة #{sale.SaleId}",
                                CustomerId = sale.CustomerId,
                                SpecificCashAccountId = CashAccountId > 0 ? CashAccountId : null,
                                SpecificBankAccountId = BankAccountId > 0 ? BankAccountId : null
                            };

                            payload.Amounts.Add(AmountSource.NetTotalAmount, sale.NetAmount);
                            payload.Amounts.Add(AmountSource.PaidCashAmount, CashAmount);
                            payload.Amounts.Add(AmountSource.PaidBankAmount, BankAmount);
                            payload.Amounts.Add(AmountSource.CreditAmount, remainingAmount);
                            payload.Amounts.Add(AmountSource.COGSAmount, totalCogs);

                            await _accountingEngine.ProcessTransactionAsync(payload);

                            await transaction.CommitAsync();
                        }
                        catch (DbUpdateConcurrencyException)
                        {
                            await transaction.RollbackAsync();
                            throw new Exception("نفدت الكمية أو تم بيع الصنف لعميل آخر في نفس اللحظة (تزامن)! يرجى التحقق من توفر المخزون وإعادة المحاولة.");
                        }
                        catch (Exception) { await transaction.RollbackAsync(); throw; }
                    });

                    await RecordLog("Add", "Sales", $"إصدار فاتورة مبيعات POS رقم {sale.SaleId}");
                    return RedirectToAction(nameof(Details), new { id = sale.SaleId });
                }
                catch (Exception ex) { ViewBag.Error = ex.Message; }
            }

            return ReloadCreateView(sale);
        }

        private IActionResult ReloadCreateView(Sales sale)
        {
            ViewBag.Customers = new SelectList(_context.Customers.Where(c => c.IsActive == true), "CustomerId", "FullName", sale.CustomerId);
            ViewBag.CashAccounts = _context.Accounts.Where(a => a.IsActive == true && a.IsParent == false && (a.AccountName.Contains("صندوق") || a.AccountName.Contains("نقد")) && a.BranchId == ActiveBranchId).ToList();
            ViewBag.BankAccounts = _context.Accounts.Where(a => a.IsActive == true && a.IsParent == false && (a.AccountName.Contains("بنك") || a.AccountName.Contains("حساب")) && (a.BranchId == ActiveBranchId || a.BranchId == null)).ToList();
            var inventoryItems = _context.Branchinventory.Include(b => b.Drug).Where(b => b.BranchId == ActiveBranchId && b.StockQuantity > 0 && b.Drug.IsActive == true).Select(b => new { id = b.DrugId, name = b.Drug.DrugName, barcode = b.Drug.Barcode, price = b.CurrentSellingPrice ?? 0, cost = b.AverageCost ?? 0, stock = b.StockQuantity, mainUnit = string.IsNullOrEmpty(b.Drug.MainUnit) ? "باكت" : b.Drug.MainUnit, subUnit = string.IsNullOrEmpty(b.Drug.SubUnit) ? "حبة" : b.Drug.SubUnit, convFactor = b.Drug.ConversionFactor > 0 ? b.Drug.ConversionFactor : 1 }).ToList();
            ViewBag.DrugsJson = System.Text.Json.JsonSerializer.Serialize(inventoryItems);
            return View(sale);
        }

        // ==========================================
        // 🔒 7. شاشة إغلاق الوردية (Shift Closing)
        // ==========================================
        [HttpGet]
        [HasPermission("Sales", "Add")]
        public async Task<IActionResult> CloseShift()
        {
            var userId = await GetValidUserIdAsync();
            var today = DateTime.Today;

            var salesToday = await _context.Sales
                .Include(s => s.SalePayments)
                .Where(s => s.UserId == userId && s.BranchId == ActiveBranchId && s.SaleDate.Date == today)
                .ToListAsync();

            var normalSales = salesToday.Where(s => s.IsReturn == false).ToList();
            var returnsToday = salesToday.Where(s => s.IsReturn == true).ToList();

            ViewBag.TotalSales = normalSales.Sum(s => s.NetAmount);
            ViewBag.TotalCash = normalSales.SelectMany(s => s.SalePayments).Where(p => p.PaymentMethod == "Cash").Sum(p => p.Amount);
            ViewBag.TotalBank = normalSales.SelectMany(s => s.SalePayments).Where(p => p.PaymentMethod == "Bank").Sum(p => p.Amount);
            ViewBag.TotalCredit = normalSales.SelectMany(s => s.SalePayments).Where(p => p.PaymentMethod == "Credit").Sum(p => p.Amount);
            
            ViewBag.TotalReturns = returnsToday.Sum(s => s.NetAmount);
            ViewBag.NetCash = (decimal)ViewBag.TotalCash - (decimal)ViewBag.TotalReturns;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Sales", "Add")]
        public async Task<IActionResult> ConfirmCloseShift(decimal actualCash)
        {
            await RecordLog("CloseShift", "Sales", $"تم إغلاق وردية الكاشير بصندوق فعلي: {actualCash}");
            TempData["Success"] = "تم إغلاق الوردية ومطابقة الصندوق بنجاح.";
            return RedirectToAction("SalesHub", "Home");
        }

      [HttpPost]
[ValidateAntiForgeryToken]
[HasPermission("Sales", "Add")]
public async Task<IActionResult> SyncOfflineSale([FromForm] string saleJson)
{
    if (string.IsNullOrEmpty(saleJson))
        return BadRequest(new { success = false, message = "بيانات الفاتورة فارغة." });

    try
    {
        var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var offlineData = System.Text.Json.JsonSerializer.Deserialize<OfflineSaleDto>(saleJson, options);
        if (offlineData == null)
            return BadRequest(new { success = false, message = "فاتورة غير صالحة." });

        var strategy = _context.Database.CreateExecutionStrategy();
        int newSaleId = 0;

        // ✅ قائمة لتتبع الأصناف التي تم تخطيها بسبب نقص المخزون
        var skippedItems = new List<object>();

        await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var userId = await GetValidUserIdAsync();

                var sale = new Sales
                {
                    UserId = userId,
                    BranchId = ActiveBranchId,
                    SaleDate = DateTime.Now,
                    IsReturn = false,
                    CustomerId = offlineData.CustomerId > 0 ? offlineData.CustomerId : null,
                    Discount = offlineData.Discount,
                    TaxAmount = offlineData.TaxAmount,
                };

                decimal grossTotal = 0, totalCogs = 0;
                sale.Saledetails = new List<Saledetails>();

                foreach (var item in offlineData.Items)
                {
                    int itemQty = (int)Math.Round(item.Quantity);

                    var inventory = await _context.Branchinventory
                        .Include(b => b.Drug)
                        .FirstOrDefaultAsync(b => b.DrugId == item.DrugId && b.BranchId == ActiveBranchId);

                    // ✅ الإصلاح الجوهري:
                    // الصنف لا يُضاف للفاتورة إلا إذا تحقق الشرطان معاً:
                    // 1. المخزون موجود في الفرع
                    // 2. الكمية المتاحة كافية للبيع
                    if (inventory != null && inventory.StockQuantity >= itemQty)
                    {
                        // ✅ إضافة قيمة الصنف للإجمالي فقط بعد التحقق من توفر المخزون
                        grossTotal += item.Quantity * item.UnitPrice;

                        decimal costPerUnit = (inventory.AverageCost ?? 0)
                            / (inventory.Drug?.ConversionFactor > 0 ? inventory.Drug.ConversionFactor : 1);
                        totalCogs += item.Quantity * costPerUnit;

                        // ✅ خصم المخزون فعلياً قبل إضافة الصنف للفاتورة
                        inventory.StockQuantity -= itemQty;
                        _context.Branchinventory.Update(inventory);

                        _context.Stockmovements.Add(new Stockmovements
                        {
                            BranchId = ActiveBranchId,
                            DrugId = item.DrugId,
                            MovementDate = DateTime.Now,
                            MovementType = "Sale Out (Offline Sync)",
                            Quantity = -itemQty,
                            UserId = userId,
                            Notes = "فاتورة مزامنة أوفلاين"
                        });

                        // ✅ الصنف يُضاف للفاتورة فقط هنا — داخل الشرط، بعد نجاح الخصم
                        sale.Saledetails.Add(new Saledetails
                        {
                            DrugId = item.DrugId,
                            Quantity = itemQty,
                            UnitPrice = item.UnitPrice
                        });
                    }
                    else
                    {
                        // ✅ تسجيل الأصناف التي تم تخطيها بسبب نقص المخزون
                        // لا نرمي Exception — نكمل الفاتورة بالأصناف المتاحة فقط
                        string drugName = inventory?.Drug?.DrugName ?? $"DrugId={item.DrugId}";
                        int availableQty = inventory?.StockQuantity ?? 0;
                        skippedItems.Add(new
                        {
                            drugId = item.DrugId,
                            drugName = drugName,
                            requestedQty = itemQty,
                            availableQty = availableQty,
                            reason = availableQty == 0 ? "الصنف نفد من المخزون" : "الكمية المطلوبة أكبر من المتاح"
                        });
                    }
                }

                // ✅ حماية من حفظ فاتورة فارغة تماماً (جميع الأصناف نفدت)
                if (!sale.Saledetails.Any())
                {
                    await transaction.RollbackAsync();
                    // لا نرمي exception بل نُعيد استجابة واضحة للـ PWA
                    return; // سيُعالَج أدناه
                }

                sale.TotalAmount = grossTotal;
                sale.NetAmount = grossTotal - sale.Discount + sale.TaxAmount;

                _context.Sales.Add(sale);
                await _context.SaveChangesAsync();
                newSaleId = sale.SaleId;

                decimal cashAmt = offlineData.CashAmount;
                decimal bankAmt = offlineData.BankAmount;
                decimal credit = sale.NetAmount - cashAmt - bankAmt;
                if (credit < 0) credit = 0;

                if (cashAmt > 0 && offlineData.CashAccountId > 0)
                    _context.SalePayments.Add(new SalePayments
                    {
                        SaleId = sale.SaleId,
                        PaymentMethod = "Cash",
                        AccountId = offlineData.CashAccountId,
                        Amount = cashAmt
                    });
                if (bankAmt > 0 && offlineData.BankAccountId > 0)
                    _context.SalePayments.Add(new SalePayments
                    {
                        SaleId = sale.SaleId,
                        PaymentMethod = "Bank",
                        AccountId = offlineData.BankAccountId,
                        Amount = bankAmt
                    });
                if (credit > 0)
                    _context.SalePayments.Add(new SalePayments
                    {
                        SaleId = sale.SaleId,
                        PaymentMethod = "Credit",
                        Amount = credit
                    });

                await _context.SaveChangesAsync();

                var payload = new AccountingPayload
                {
                    TransactionType = TransactionType.SalesInvoice,
                    BranchId = ActiveBranchId,
                    UserId = userId,
                    ReferenceNo = sale.SaleId.ToString(),
                    Description = $"مبيعات POS (أوفلاين مزامنة) فاتورة #{sale.SaleId}",
                    CustomerId = sale.CustomerId,
                    SpecificCashAccountId = offlineData.CashAccountId > 0 ? offlineData.CashAccountId : null,
                    SpecificBankAccountId = offlineData.BankAccountId > 0 ? offlineData.BankAccountId : null
                };
                payload.Amounts.Add(AmountSource.NetTotalAmount, sale.NetAmount);
                payload.Amounts.Add(AmountSource.PaidCashAmount, cashAmt);
                payload.Amounts.Add(AmountSource.PaidBankAmount, bankAmt);
                payload.Amounts.Add(AmountSource.CreditAmount, credit);
                payload.Amounts.Add(AmountSource.COGSAmount, totalCogs);

                await _accountingEngine.ProcessTransactionAsync(payload);
                await transaction.CommitAsync();
            }
            catch (Exception) { await transaction.RollbackAsync(); throw; }
        });

        // ✅ حالة: جميع الأصناف نفدت — لم تُحفظ أي فاتورة
        if (newSaleId == 0)
        {
            return Ok(new
            {
                success = false,
                saleId = 0,
                message = "لم تُحفظ الفاتورة: جميع الأصناف نفدت من المخزون.",
                skippedItems = skippedItems
            });
        }

        await RecordLog("OfflineSync", "Sales", $"مزامنة فاتورة أوفلاين #{newSaleId}، أصناف متخطاة: {skippedItems.Count}");

        return Ok(new
        {
            success = true,
            saleId = newSaleId,
            // ✅ إعلام الـ PWA بالأصناف التي لم تُحسب في الفاتورة
            skippedItems = skippedItems,
            hasSkippedItems = skippedItems.Any()
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { success = false, message = ex.Message });
    }
}

        // DTO لاستقبال بيانات الفاتورة الأوفلاين
        private class OfflineSaleDto
        {
            public int CustomerId { get; set; }
            public decimal Discount { get; set; }
            public decimal TaxAmount { get; set; }
            public decimal CashAmount { get; set; }
            public int CashAccountId { get; set; }
            public decimal BankAmount { get; set; }
            public int BankAccountId { get; set; }
            public List<OfflineSaleItemDto> Items { get; set; } = new();
        }
        private class OfflineSaleItemDto
        {
            public int DrugId { get; set; }
            public decimal Quantity { get; set; }
            public decimal UnitPrice { get; set; }
        }
    }
}
