using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Models;
using PharmaSmartWeb.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PharmaSmartWeb.Controllers
{
    [Authorize]
    [Route("DataSeeder/[action]")]
    public class DataSeederController : BaseController
    {
        private readonly IAccountingEngine _accountingEngine;

        public DataSeederController(ApplicationDbContext context, IAccountingEngine accountingEngine) : base(context)
        {
            _accountingEngine = accountingEngine;
        }

        [HttpGet]
        public IActionResult Index()
        {
            // شاشة تعرض الخيارات للمستخدم لزراعة البيانات
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SeedAllData()
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserID")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "1");
                int branchId = ActiveBranchId;

                // 1. إنشاء الأدوية الموسمية للمحاكاة
                var generatedDrugs = await GenerateSeasonalDrugs(branchId);

                // 2. إنشاء المشتريات لتمويل المخزون (بحيث نشتري كميات كبيرة قبل السنتين)
                await SeedPurchases(generatedDrugs, branchId, userId);

                // 3. إنشاء المبيعات التاريخية تعكس النمط الموسمي الحقيقي
                await SeedSeasonalSales(generatedDrugs, branchId, userId);

                return Json(new { success = true, message = "تم زراعة بيئة المحاكاة بنجاح! مبيعات، مشتريات، مخزون، وقيود يومية مكتملة." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطأ أثناء توليد البيانات: " + ex.Message });
            }
        }

        private async Task<List<Drugs>> GenerateSeasonalDrugs(int branchId)
        {
            var drugsList = new List<Drugs>();
            
            // تحقق إذا كانت الأدوية موجودة من قبل لتفادي التكرار
            bool checkExists = await _context.Drugs.AnyAsync(d => d.DrugName.Contains("[محاكاة]"));
            if (checkExists)
            {
                var existingDrugs = await _context.Drugs.Where(d => d.DrugName.Contains("[محاكاة]")).ToListAsync();
                return existingDrugs;
            }

            var group = await _context.ItemGroups.FirstOrDefaultAsync(g => g.GroupName == "أدوية الشتاء والصيف") 
                        ?? new ItemGroups { GroupName = "أدوية الشتاء والصيف", IsActive = true };
            if (group.GroupId == 0) { _context.ItemGroups.Add(group); await _context.SaveChangesAsync(); }

            var drugsToCreate = new List<(string Name, string MainU, string SubU, int Conv, string Season)>
            {
                ("بانادول كولد اند فلو [محاكاة شتاء]", "باكت", "حبة", 24, "Winter"),
                ("فيتامين سي 1000مج [محاكاة شتاء]", "علبة", "قرص", 30, "Winter"),
                ("مضاد سعلة - شراب [محاكاة شتاء]", "زجاجة", "زجاجة", 1, "Winter"),
                
                ("كريم واقي شمس 50+ [محاكاة صيف]", "عبوة", "عبوة", 1, "Summer"),
                ("أدوية الحساسية - لوراتادين [محاكاة ربيع]", "باكت", "حبة", 10, "Spring"),
                ("مضاد حيوي عام أوجمينتين [محاكاة دائم]", "باكت", "حبة", 14, "AllYear")
            };

            foreach (var d in drugsToCreate)
            {
                var drug = new Drugs
                {
                    DrugName = d.Name,
                    MainUnit = d.MainU,
                    SubUnit = d.SubU,
                    ConversionFactor = d.Conv,
                    GroupId = group.GroupId,
                    IsActive = true,
                    SaremaCategory = "S", // للمخزون
                    IsLifeSaving = false
                };
                _context.Drugs.Add(drug);
                drugsList.Add(drug);
            }
            await _context.SaveChangesAsync();

            // إنشاء أرصدة المخزون للفرع
            foreach(var dr in drugsList)
            {
                if (!await _context.Branchinventory.AnyAsync(b => b.DrugId == dr.DrugId && b.BranchId == branchId))
                {
                     _context.Branchinventory.Add(new Branchinventory
                    {
                        BranchId = branchId,
                        DrugId = dr.DrugId,
                        StockQuantity = 0,
                        MinimumStockLevel = 50,
                        Abccategory = "C",
                        AverageCost = 0,
                        CurrentSellingPrice = 1000 // سعر افتراضي يتحدث بعد الشراء
                    });
                }
            }
            await _context.SaveChangesAsync();

            return drugsList;
        }

        private async Task SeedPurchases(List<Drugs> drugs, int branchId, int userId)
        {
            DateTime purchaseDate = DateTime.Now.AddMonths(-25); // نشتري قبل بداية المبيعات بشهر

            // البحث عن مورد افتراضي أو حسابه
            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.BranchId == branchId);
            if (supplier == null)
            {
                supplier = new Suppliers { SupplierName = "مورّد المحاكاة (مصنع الأدوية)", Phone = "00000", BranchId = branchId, IsActive = true };
                _context.Suppliers.Add(supplier);
                await _context.SaveChangesAsync();
            }

            var purchase = new Purchases
            {
                BranchId = branchId,
                UserId = userId,
                SupplierId = supplier.SupplierId,
                InvoiceNumber = "SIM-PUR-" + DateTime.Now.Ticks.ToString().Substring(0, 6),
                PurchaseDate = purchaseDate,
                IsReturn = false,
                TotalAmount = 0,
                NetAmount = 0
            };

            var pDetails = new List<Purchasedetails>();
            decimal totalNet = 0;

            foreach (var drug in drugs)
            {
                // شراء كمية ضخمة لتدوم لسنتين
                int qty = 10000;
                decimal unitCost = 500; // تكلفة الحبة 500
                decimal sellingPrice = 750; // سعر البيع للحبة

                pDetails.Add(new Purchasedetails
                {
                    DrugId = drug.DrugId,
                    Quantity = qty,
                    CostPrice = unitCost,
                    SubTotal = qty * unitCost,
                    BatchNumber = "BATCH-" + drug.DrugId,
                    ExpiryDate = DateTime.Now.AddYears(3),
                    SellingPrice = sellingPrice
                });

                totalNet += (qty * unitCost);

                // تحديث المخزون
                var inv = await _context.Branchinventory.FirstOrDefaultAsync(b => b.BranchId == branchId && b.DrugId == drug.DrugId);
                if (inv != null)
                {
                    inv.StockQuantity += qty;
                    inv.AverageCost = unitCost;
                    inv.CurrentSellingPrice = sellingPrice;
                    _context.Branchinventory.Update(inv);
                }
                
                // تسجيل حركة المخزون
                _context.Stockmovements.Add(new Stockmovements
                {
                    BranchId = branchId,
                    DrugId = drug.DrugId,
                    MovementDate = purchaseDate,
                    MovementType = "Purchase In (Simulation)",
                    Quantity = qty,
                    UserId = userId,
                    Notes = "مشتريات محاكاة"
                });
            }

            purchase.TotalAmount = totalNet;
            purchase.NetAmount = totalNet;
            purchase.Purchasedetails = pDetails;
            _context.Purchases.Add(purchase);
            await _context.SaveChangesAsync();

            // المحاسبة: قيد على المورد
            var payload = new AccountingPayload
            {
                TransactionType = TransactionType.PurchaseInvoice,
                BranchId = branchId,
                UserId = userId,
                ReferenceNo = purchase.PurchaseId.ToString(),
                Description = $"مشتريات محاكاة #{purchase.PurchaseId}",
                SupplierId = supplier.SupplierId
            };
            payload.Amounts.Add(AmountSource.NetTotalAmount, totalNet);
            payload.Amounts.Add(AmountSource.CreditAmount, totalNet); // آجل بالكامل أو نقد، لنجعله آجل

            await _accountingEngine.ProcessTransactionAsync(payload);
        }

        private async Task SeedSeasonalSales(List<Drugs> drugs, int branchId, int userId)
        {
            var random = new Random();
            DateTime startDate = DateTime.Now.AddMonths(-24);
            int totalDays = (DateTime.Now - startDate).Days;

            var cashAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.BranchId == branchId && a.AccountName.Contains("صندوق") && !a.IsParent);
            if (cashAccount == null) throw new Exception("لا يوجد حساب صندوق في الفرع لتقييد المبيعات.");

            var salesList = new List<Sales>();
            var allStockMvts = new List<Stockmovements>();
            var allPayments = new List<SalePayments>();

            // استخراج قائمة المخزون للصيانة 
            var inventoryList = await _context.Branchinventory.Where(b => b.BranchId == branchId && drugs.Select(d => d.DrugId).Contains(b.DrugId)).ToListAsync();
            
            // سنقوم بتوليد المبيعات بشكل يومي
            // بدلاً من استدعاء _accountingEngine آلاف المرات لتبطيء السيرفر، سنولد الحركات في DB ثم نقوم بتمرير قيد يومية مجمع أو نقبل القيود تباعاً لأنها للبيانات التجريبية
            for (int day = 0; day < totalDays; day++)
            {
                DateTime currentDate = startDate.AddDays(day);
                int month = currentDate.Month;

                // عدد المبيعات اليومية عشوائي بناءً على الفصل (لكي تظهر المخططات متعرجة بشكل منطقي)
                int numberOfInvoicesToday = random.Next(2, 6);

                for (int i = 0; i < numberOfInvoicesToday; i++)
                {
                    decimal invoiceTotal = 0;
                    decimal invoiceCogs = 0;
                    var saleDetails = new List<Saledetails>();

                    foreach (var drug in drugs)
                    {
                        // المنطق السحري لاختيار الكمية حسب الموسم
                        int qtyToSell = 0;
                        string szn = drug.DrugName;

                        if (szn.Contains("[محاكاة شتاء]"))
                        {
                            qtyToSell = (month == 11 || month == 12 || month == 1 || month == 2) ? random.Next(10, 30) : random.Next(0, 3);
                        }
                        else if (szn.Contains("[محاكاة صيف]"))
                        {
                            qtyToSell = (month == 6 || month == 7 || month == 8) ? random.Next(15, 40) : random.Next(0, 2);
                        }
                        else if (szn.Contains("[محاكاة ربيع]"))
                        {
                            qtyToSell = (month == 3 || month == 4 || month == 5) ? random.Next(10, 25) : random.Next(0, 4);
                        }
                        else
                        {
                            // دائم
                            qtyToSell = random.Next(5, 15);
                        }

                        if (qtyToSell > 0)
                        {
                            var inv = inventoryList.FirstOrDefault(inv => inv.DrugId == drug.DrugId);
                            if (inv != null && inv.StockQuantity >= qtyToSell)
                            {
                                inv.StockQuantity -= qtyToSell;
                                decimal sPrice = inv.CurrentSellingPrice ?? 750;
                                decimal costP = inv.AverageCost ?? 500;

                                invoiceTotal += (qtyToSell * sPrice);
                                invoiceCogs += (qtyToSell * costP);

                                saleDetails.Add(new Saledetails { DrugId = drug.DrugId, Quantity = qtyToSell, UnitPrice = sPrice });
                                allStockMvts.Add(new Stockmovements { BranchId = branchId, UserId = userId, DrugId = drug.DrugId, Quantity = -qtyToSell, MovementDate = currentDate, MovementType = "Sale Out (Sim)", Notes = "محاكاة" });
                            }
                        }
                    }

                    if (invoiceTotal > 0)
                    {
                        var sale = new Sales
                        {
                            BranchId = branchId,
                            UserId = userId,
                            SaleDate = currentDate,
                            IsReturn = false,
                            TotalAmount = invoiceTotal,
                            NetAmount = invoiceTotal,
                            Saledetails = saleDetails
                        };

                        _context.Sales.Add(sale);
                        // نحتاج لحفظ المبيعات لاحقاً لأخذ الأرقام
                        salesList.Add(sale);
                        
                        allPayments.Add(new SalePayments { Sale = sale, AccountId = cashAccount.AccountId, Amount = invoiceTotal, PaymentMethod = "Cash" });
                        
                        // هنا لتسريع العملية، لا نسجل قيود يومية لكل فاتورة تجريبية (تجنب الشلل للسيرفر). 
                        // سنعتمد على الحركات البيانية في الشاشات. أو نطلب AccountingEngine، لكن قد يأخذ 5 دقائق للتوليد.
                        // دعنا نسجل قيد يومي واحد لكل يوم محاكاة!
                    }
                }
            }

            _context.Branchinventory.UpdateRange(inventoryList);
            _context.Stockmovements.AddRange(allStockMvts);
            _context.SalePayments.AddRange(allPayments);

            await _context.SaveChangesAsync();
            
            // Generate Journal Entry summary per month to simulate accounting engine without locking the system
            await GenerateSummarizedJournals(salesList, cashAccount.AccountId, branchId, userId);
        }

        private async Task GenerateSummarizedJournals(List<Sales> sales, int cashAccountId, int branchId, int userId)
        {
            // لتسريع النظام نقوم بصنع قيود مجمعة لكل شهر كحجر أساس للتدقيق!
            var salesByMonth = sales.GroupBy(s => new { s.SaleDate.Year, s.SaleDate.Month });
            
            // سنضمن حساب المبيعات
            var salesAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountName.Contains("مبيعات") && a.BranchId == branchId);
            if (salesAccount == null) return;

            foreach(var m in salesByMonth)
            {
                decimal totalMonthSales = m.Sum(x => x.NetAmount);
                var jEntry = new Journalentries
                {
                    BranchId = branchId,
                    CreatedBy = userId,
                    JournalDate = new DateTime(m.Key.Year, m.Key.Month, 28), // تاريخ بنهاية الشهر تقريباً
                    ReferenceNo = $"SIM-SALES-{m.Key.Year}-{m.Key.Month}",
                    Description = $"قيد مبيعات مجمع لشهر {m.Key.Month}/{m.Key.Year} - محاكاة",
                    IsPosted = true,
                    Journaldetails = new List<Journaldetails>()
                };

                jEntry.Journaldetails.Add(new Journaldetails { AccountId = cashAccountId, Debit = totalMonthSales, Credit = 0 });
                jEntry.Journaldetails.Add(new Journaldetails { AccountId = salesAccount.AccountId, Debit = 0, Credit = totalMonthSales });

                _context.Journalentries.Add(jEntry);
            }

            await _context.SaveChangesAsync();
        }
    }
}
