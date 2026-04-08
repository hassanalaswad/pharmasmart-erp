using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Models;
using PharmaSmartWeb.Filters;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PharmaSmartWeb.Controllers
{
    [Authorize]
    public class DrugTransfersController : BaseController
    {
        public DrugTransfersController(ApplicationDbContext context) : base(context) { }

        // ==========================================
        // 📦 1. لوحة تحكم التحويلات (صادرة وواردة)
        // ==========================================
        [HttpGet]
        [HasPermission("DrugTransfers", "View")]
        public async Task<IActionResult> Index()
        {
            var currentBranch = ActiveBranchId;

            var transfers = await _context.Drugtransfers
                .Include(t => t.FromBranch)
                .Include(t => t.ToBranch)
                .Include(t => t.CreatedByNavigation)
                .Include(t => t.Drugtransferdetails)
                .Where(t => t.FromBranchId == currentBranch || t.ToBranchId == currentBranch)
                .OrderByDescending(t => t.TransferDate)
                .ToListAsync();

            ViewBag.CurrentBranchId = currentBranch;
            return View(transfers);
        }

        // ==========================================
        // ➕ 2. شاشة إصدار بضاعة لفرع آخر
        // ==========================================
        [HttpGet]
        [HasPermission("DrugTransfers", "Add")]
        public async Task<IActionResult> Create()
        {
            var branches = await _context.Branches.Where(b => b.IsActive == true && b.BranchId != ActiveBranchId).ToListAsync();
            ViewBag.Branches = new SelectList(branches, "BranchId", "BranchName");

            var inventoryItems = await _context.Branchinventory
                .Include(b => b.Drug)
                .Where(b => b.BranchId == ActiveBranchId && b.StockQuantity > 0 && b.Drug.IsActive == true)
                .Select(b => new {
                    id = b.DrugId,
                    name = b.Drug.DrugName,
                    barcode = b.Drug.Barcode,
                    cost = b.AverageCost ?? 0,
                    stock = b.StockQuantity,
                    mainUnit = string.IsNullOrEmpty(b.Drug.MainUnit) ? "باكت" : b.Drug.MainUnit,
                    subUnit = string.IsNullOrEmpty(b.Drug.SubUnit) ? "حبة" : b.Drug.SubUnit,
                    convFactor = b.Drug.ConversionFactor > 0 ? b.Drug.ConversionFactor : 1
                }).ToListAsync();

            ViewBag.DrugsJson = System.Text.Json.JsonSerializer.Serialize(inventoryItems);
            return View();
        }

        // ==========================================
        // 🚚 3. إرسال البضاعة (المرحلة الأولى)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("DrugTransfers", "Add")]
        public async Task<IActionResult> ProcessTransfer(int ToBranchId, string Notes, List<Drugtransferdetails> Details)
        {
            if (Details == null || !Details.Any())
            {
                TempData["Error"] = "يجب اختيار صنف واحد على الأقل.";
                return RedirectToAction(nameof(Create));
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        int userId = int.Parse(User.FindFirst("UserID")?.Value ?? "1");
                        decimal totalTransferCost = 0;

                        // 1. إنشاء أمر التحويل
                        var transfer = new Drugtransfers
                        {
                            FromBranchId = ActiveBranchId,
                            ToBranchId = ToBranchId,
                            TransferDate = DateTime.Now,
                            Status = "Pending", // 🚀 الحالة الافتراضية
                            Notes = Notes,
                            CreatedBy = userId
                        };
                        _context.Drugtransfers.Add(transfer);
                        await _context.SaveChangesAsync();

                        // 2. معالجة الأصناف وخصمها من الفرع المُرسل
                        foreach (var item in Details)
                        {
                            var inventory = await _context.Branchinventory
                                .Include(b => b.Drug)
                                .FirstOrDefaultAsync(b => b.DrugId == item.DrugId && b.BranchId == ActiveBranchId);
                            if (inventory == null || inventory.StockQuantity < item.Quantity)
                                throw new Exception($"الكمية غير متوفرة للصنف المختار في مخزنك.");

                            item.TransferId = transfer.TransferId;
                            decimal unitCost = (inventory.AverageCost ?? 0) / (inventory.Drug?.ConversionFactor > 0 ? inventory.Drug.ConversionFactor : 1);
                            item.UnitCost = unitCost;
                            totalTransferCost += (item.Quantity * unitCost);

                            _context.Drugtransferdetails.Add(item);

                            inventory.StockQuantity -= item.Quantity;
                            _context.Branchinventory.Update(inventory);

                            _context.Stockmovements.Add(new Stockmovements { BranchId = ActiveBranchId, DrugId = item.DrugId, MovementDate = DateTime.Now, MovementType = "Transfer Out", Quantity = -item.Quantity, UserId = userId, Notes = $"إرسال بضاعة للفرع {ToBranchId}" });
                        }

                        // 3. القيد المحاسبي الأول (الإرسال)
                        var inTransitAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountCode == "114");
                        var inventoryAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountCode == "113" || a.AccountName.Contains("مخزون بضاعة"));

                        if (inTransitAccount != null && inventoryAccount != null && totalTransferCost > 0)
                        {
                            var journal = new Journalentries { JournalDate = DateTime.Now, ReferenceType = "StockTransferOut", ReferenceNo = transfer.TransferId.ToString(), Description = $"تحويل مخزني صادر للفرع {ToBranchId}", BranchId = ActiveBranchId, CreatedBy = userId, IsPosted = true };
                            _context.Journalentries.Add(journal);
                            await _context.SaveChangesAsync();

                            _context.Journaldetails.Add(new Journaldetails { JournalId = journal.JournalId, AccountId = inTransitAccount.AccountId, Debit = totalTransferCost, Credit = 0 });
                            _context.Journaldetails.Add(new Journaldetails { JournalId = journal.JournalId, AccountId = inventoryAccount.AccountId, Debit = 0, Credit = totalTransferCost });

                            transfer.JournalId = journal.JournalId;
                            await _context.SaveChangesAsync();
                        }

                        await transaction.CommitAsync();
                    }
                    catch (Exception) { await transaction.RollbackAsync(); throw; }
                });

                TempData["Success"] = "تم إصدار البضاعة بنجاح وهي الآن (في الطريق).";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Create));
            }
        }

        // ==========================================
        // ✅ 4. استلام البضاعة (المرحلة الثانية)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("DrugTransfers", "Edit")]
        public async Task<IActionResult> ReceiveTransfer(int id)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        var transfer = await _context.Drugtransfers
                            .Include(t => t.Drugtransferdetails)
                            .FirstOrDefaultAsync(t => t.TransferId == id && t.ToBranchId == ActiveBranchId && t.Status == "Pending");

                        if (transfer == null) throw new Exception("أمر التحويل غير متاح أو تم استلامه مسبقاً.");

                        int userId = int.Parse(User.FindFirst("UserID")?.Value ?? "1");
                        decimal totalReceivedCost = 0;

                        foreach (var item in transfer.Drugtransferdetails)
                        {
                            totalReceivedCost += (item.Quantity * item.UnitCost);

                            var inventory = await _context.Branchinventory
                                .Include(b => b.Drug)
                                .FirstOrDefaultAsync(b => b.DrugId == item.DrugId && b.BranchId == ActiveBranchId);
                            if (inventory != null)
                            {
                                decimal currentQty = inventory.StockQuantity > 0 ? inventory.StockQuantity : 0;
                                decimal currentTotalCost = currentQty * (inventory.AverageCost ?? 0);
                                decimal incomingTotalCost = item.Quantity * item.UnitCost;
                                decimal newTotalQty = currentQty + item.Quantity;

                                if (newTotalQty > 0)
                                    inventory.AverageCost = (currentTotalCost + incomingTotalCost) / newTotalQty;

                                inventory.StockQuantity += item.Quantity;
                                _context.Branchinventory.Update(inventory);
                            }
                            else
                            {
                                _context.Branchinventory.Add(new Branchinventory { BranchId = ActiveBranchId, DrugId = item.DrugId, StockQuantity = item.Quantity, AverageCost = item.UnitCost, MinimumStockLevel = 10 });
                            }

                            _context.Stockmovements.Add(new Stockmovements { BranchId = ActiveBranchId, DrugId = item.DrugId, MovementDate = DateTime.Now, MovementType = "Transfer In", Quantity = item.Quantity, UserId = userId, Notes = $"استلام بضاعة من الفرع {transfer.FromBranchId}" });
                        }

                        var inTransitAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountCode == "114");
                        var inventoryAccount = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountCode == "113" || a.AccountName.Contains("مخزون بضاعة"));

                        if (inTransitAccount != null && inventoryAccount != null && totalReceivedCost > 0)
                        {
                            var journal = new Journalentries { JournalDate = DateTime.Now, ReferenceType = "StockTransferIn", ReferenceNo = transfer.TransferId.ToString(), Description = $"استلام مخزني وارد من الفرع {transfer.FromBranchId}", BranchId = ActiveBranchId, CreatedBy = userId, IsPosted = true };
                            _context.Journalentries.Add(journal);
                            await _context.SaveChangesAsync();

                            _context.Journaldetails.Add(new Journaldetails { JournalId = journal.JournalId, AccountId = inventoryAccount.AccountId, Debit = totalReceivedCost, Credit = 0 });
                            _context.Journaldetails.Add(new Journaldetails { JournalId = journal.JournalId, AccountId = inTransitAccount.AccountId, Debit = 0, Credit = totalReceivedCost });

                            transfer.ReceiptJournalId = journal.JournalId;
                        }

                        transfer.Status = "Received";
                        transfer.ReceiveDate = DateTime.Now;
                        transfer.ReceivedBy = userId;

                        _context.Drugtransfers.Update(transfer);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                    catch (Exception) { await transaction.RollbackAsync(); throw; }
                });

                TempData["Success"] = "تم استلام البضاعة وتحديث أرصدة المخزن بنجاح.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}