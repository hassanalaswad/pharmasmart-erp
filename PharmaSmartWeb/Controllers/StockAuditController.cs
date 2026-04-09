using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Models;
using PharmaSmartWeb.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PharmaSmartWeb.Controllers
{
    [Authorize]
    public class StockAuditController : BaseController
    {
        public StockAuditController(ApplicationDbContext context) : base(context) { }

        // ==========================================
        // 📄 1. عرض سجل عمليات الجرد السابقة (معزول)
        // ==========================================
        [HasPermission("Inventory", "View")]
        public async Task<IActionResult> Index()
        {
            var audits = await _context.Stockaudits
                .Include(a => a.User)
                .Where(a => a.BranchId == ActiveBranchId) // 🚀 عزل الفرع النشط
                .OrderByDescending(a => a.AuditDate)
                .ToListAsync();
            return View(audits);
        }

        // ==========================================
        // ➕ 2. بدء عملية جرد جديدة (تحميل مخزن الفرع)
        // ==========================================
        [HttpGet]
        [HasPermission("Inventory", "Add")]
        public async Task<IActionResult> Create()
        {
            // جلب كافة الأصناف التي لها رصيد (أو معرفة) في هذا الفرع النشط
            var inventoryItems = await _context.Branchinventory
                .Include(bi => bi.Drug)
                .Where(bi => bi.BranchId == ActiveBranchId)
                .Select(bi => new StockAuditDetailViewModel
                {
                    DrugId = bi.DrugId,
                    DrugName = bi.Drug.DrugName,
                    Barcode = bi.Drug.Barcode,
                    SystemQty = bi.StockQuantity,
                }).ToListAsync();

            ViewBag.BranchName = (await _context.Branches.FindAsync(ActiveBranchId))?.BranchName;
            return View(inventoryItems);
        }

        // ==========================================
        // 💾 3. معالجة الجرد وتوليد فاتورة التسوية (POST)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Inventory", "Add")]
        public async Task<IActionResult> Create(List<StockAuditDetailViewModel> auditResults, string notes)
        {
            if (auditResults == null || !auditResults.Any())
            {
                TempData["Error"] = "لا توجد بيانات جرد لإتمام العملية.";
                return RedirectToAction(nameof(Create));
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    using (var transaction = await _context.Database.BeginTransactionAsync())
                    {
                        try
                        {
                            int userId = int.Parse(User.FindFirst("UserID")?.Value ?? "1");

                            // 1️⃣ إنشاء رأس مستند الجرد
                            var auditMaster = new Stockaudits
                            {
                                BranchId = ActiveBranchId,
                                AuditDate = DateTime.Now,
                                UserId = userId,
                                Notes = notes,
                                Status = "Completed"
                            };
                            _context.Stockaudits.Add(auditMaster);
                            await _context.SaveChangesAsync();

                            decimal totalDeficitValue = 0; // إجمالي قيمة العجز (خسارة)
                            decimal totalSurplusValue = 0; // إجمالي قيمة الفائض (ربح)

                            foreach (var item in auditResults)
                            {
                                int difference = item.PhysicalQty - item.SystemQty;
                                if (difference == 0) continue; // لا يوجد تغيير

                                // 2️⃣ تسجيل تفاصيل الجرد
                                var detail = new Stockauditdetails
                                {
                                    AuditId = auditMaster.AuditId,
                                    DrugId = item.DrugId,
                                    SystemQty = item.SystemQty,
                                    PhysicalQty = item.PhysicalQty,
                                    Difference = difference,
                                    UnitCost = item.UnitCost
                                };
                                _context.Stockauditdetails.Add(detail);

                                // 3️⃣ تحديث رصيد المخزون الفعلي للفرع
                                var inventory = await _context.Branchinventory
                                    .FirstOrDefaultAsync(bi => bi.BranchId == ActiveBranchId && bi.DrugId == item.DrugId);

                                if (inventory != null)
                                {
                                    inventory.StockQuantity = item.PhysicalQty;
                                }

                                // 4️⃣ تسجيل حركة مخزنية (تسوية)
                                _context.Stockmovements.Add(new Stockmovements
                                {
                                    BranchId = ActiveBranchId,
                                    DrugId = item.DrugId,
                                    MovementDate = DateTime.Now,
                                    MovementType = difference > 0 ? "Adjustment In" : "Adjustment Out",
                                    Quantity = Math.Abs(difference),
                                    ReferenceId = auditMaster.AuditId,
                                    UserId = userId,
                                    Notes = $"تسوية جرد مخزني - مستند رقم #{auditMaster.AuditId}"
                                });

                                // حساب القيم المالية للتسوية
                                if (difference < 0) totalDeficitValue += Math.Abs(difference) * item.UnitCost;
                                else totalSurplusValue += difference * item.UnitCost;
                            }

                            // 🧠 5️⃣ التوجيه المحاسبي (قيد تسوية المخزون)
                            decimal netAdjustment = totalSurplusValue - totalDeficitValue;

                            if (netAdjustment != 0)
                            {
                                var journal = new Journalentries
                                {
                                    JournalDate = DateTime.Now,
                                    Description = $"قيد تسوية جرد فرع {ActiveBranchId} - مستند #{auditMaster.AuditId}",
                                    BranchId = ActiveBranchId,
                                    CreatedBy = userId,
                                    IsPosted = true,
                                    ReferenceType = "Adjustment"
                                };
                                _context.Journalentries.Add(journal);
                                await _context.SaveChangesAsync();

                                // حساب المخزون (10)
                                var inventoryAcc = await _context.Accounts.FindAsync(10);
                                // حساب تسوية المخزون (الحساب الجديد 515)
                                var adjustmentAcc = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountCode == "515");

                                if (netAdjustment > 0) // فائض (زيادة في الأصول)
                                {
                                    _context.Journaldetails.Add(new Journaldetails { JournalId = journal.JournalId, AccountId = 10, Debit = netAdjustment, Credit = 0 });
                                    if (adjustmentAcc != null) _context.Journaldetails.Add(new Journaldetails { JournalId = journal.JournalId, AccountId = adjustmentAcc.AccountId, Credit = netAdjustment, Debit = 0 });
                                    if (inventoryAcc != null) inventoryAcc.Balance += netAdjustment;
                                    if (adjustmentAcc != null) adjustmentAcc.Balance -= netAdjustment; // دائن في المصروفات يقلل التكلفة
                                }
                                else // عجز (خسارة)
                                {
                                    decimal absNet = Math.Abs(netAdjustment);
                                    if (adjustmentAcc != null) _context.Journaldetails.Add(new Journaldetails { JournalId = journal.JournalId, AccountId = adjustmentAcc.AccountId, Debit = absNet, Credit = 0 });
                                    _context.Journaldetails.Add(new Journaldetails { JournalId = journal.JournalId, AccountId = 10, Credit = absNet, Debit = 0 });
                                    if (inventoryAcc != null) inventoryAcc.Balance -= absNet;
                                    if (adjustmentAcc != null) adjustmentAcc.Balance += absNet;
                                }
                            }

                            await _context.SaveChangesAsync();
                            await transaction.CommitAsync();

                            await RecordLog("Add", "Inventory", $"إتمام عملية جرد مخزني رقم #{auditMaster.AuditId} للفرع {ActiveBranchId}. صافي التسوية: {netAdjustment}");
                            TempData["Success"] = "تم حفظ الجرد، وتحديث المخازن، وترحيل قيود التسوية المالية بنجاح.";
                        }
                        catch (Exception) { await transaction.RollbackAsync(); throw; }
                    }
                });
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Error = "خطأ فني في عملية الجرد: " + ex.Message;
                return View(auditResults);
            }
        }

        // ==========================================
        // 👁️ 4. عرض تفاصيل عملية جرد سابقة
        // ==========================================
        [HasPermission("Inventory", "View")]
        public async Task<IActionResult> Details(int id)
        {
            var audit = await _context.Stockaudits
                .Include(a => a.User)
                .Include(a => a.Stockauditdetails).ThenInclude(d => d.Drug)
                .FirstOrDefaultAsync(a => a.AuditId == id);

            if (audit == null || audit.BranchId != ActiveBranchId) return NotFound();

            return View(audit);
        }
    }

    // ViewModel بسيط لنقل البيانات من وإلى واجهة الجرد
    public class StockAuditDetailViewModel
    {
        public int DrugId { get; set; }
        public string DrugName { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public int SystemQty { get; set; }
        public int PhysicalQty { get; set; }
        public decimal UnitCost { get; set; }
    }
}

/* =============================================================================================
📑 الكتالوج والدليل الفني للكنترولر (StockAuditController)
=============================================================================================
الوظيفة العامة: 
هذا الكنترولر هو "صمام الأمان المخزني" (Stock Integrity Engine).
يختص بعمليات الجرد الدوري والمفاجئ، ومقارنة الأرصدة الدفترية بالأرصدة الفعلية في الرفوف.

ملاحظة معمارية بخصوص التسوية (Adjustment Logic):
1. العزل الشامل: يتم الجرد والتسوية حصراً داخل سياق `ActiveBranchId` لضمان عدم تأثير جرد 
   فرع على مخزون فرع آخر.
2. التصحيح المخزني التلقائي: بمجرد إدخال "الكمية الفعلية"، يقوم النظام بحساب الفرق وتحديث 
   جدول `Branchinventory` فوراً ليتطابق مع الواقع، مع تسجيل حركة مخزنية من نوع 
   `Adjustment` للرقابة.
3. الأثر المالي (Financial Integration): لا يكتفي النظام بتعديل الكميات، بل يقوم بإنشاء 
   قيد محاسبي مزدوج يثبت "عجز المخزون" كمصروف أو "فائض المخزون" كإيراد تسوية، مما يضمن 
   أن قيمة الأصول في الميزانية (حساب 10) مطابقة تماماً للقيمة الفعلية للبضاعة في المخازن.

سجل الرقابة:
يتم توثيق عملية الجرد في `SystemLogs` مع توضيح "صافي قيمة التسوية" للمراقبين الماليين.
=============================================================================================
*/
