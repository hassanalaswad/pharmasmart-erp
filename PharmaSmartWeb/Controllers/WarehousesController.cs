using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Models;
using PharmaSmartWeb.Filters;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

namespace PharmaSmartWeb.Controllers
{
    [Authorize]
    public class WarehousesController : BaseController
    {
        public WarehousesController(ApplicationDbContext context) : base(context) { }

        // =======================================================
        // 1. عرض قائمة المستودعات
        // =======================================================
        [HttpGet]
        [HasPermission("Warehouses", "View")]
        public async Task<IActionResult> Index()
        {
            var warehouses = await _context.Warehouses
                .Include(w => w.Shelves)
                .Where(w => w.BranchId == ActiveBranchId)
                .ToListAsync();
            return View(warehouses);
        }

        // =======================================================
        // 2. شاشة إضافة مستودع جديد
        // =======================================================
        [HttpGet]
        [HasPermission("Warehouses", "Add")]
        public async Task<IActionResult> Create()
        {
            // جلب المجموعات العلاجية للرفوف وتمريرها كـ JSON
            var groups = await _context.ItemGroups.Where(g => g.IsActive).Select(g => new { id = g.GroupId, name = g.GroupName }).ToListAsync();
            ViewBag.ItemGroupsJson = JsonSerializer.Serialize(groups);

            return View(new Warehouses { IsActive = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Warehouses", "Add")]
        public async Task<IActionResult> Create(Warehouses warehouse)
        {
            ModelState.Remove("Branch"); // لتفادي أخطاء الـ Validation

            if (ModelState.IsValid)
            {
                // 🚀 الحل المعماري: استخدام استراتيجية التنفيذ الآمنة
                var strategy = _context.Database.CreateExecutionStrategy();
                try
                {
                    await strategy.ExecuteAsync(async () =>
                    {
                        using (var transaction = await _context.Database.BeginTransactionAsync())
                        {
                            try
                            {
                                warehouse.BranchId = ActiveBranchId;

                                _context.Warehouses.Add(warehouse);
                                await _context.SaveChangesAsync();

                                await transaction.CommitAsync();
                            }
                            catch (Exception)
                            {
                                await transaction.RollbackAsync();
                                throw;
                            }
                        }
                    });

                    await RecordLog("Add", "Warehouses", $"إنشاء مستودع جديد: {warehouse.WarehouseName}");
                    TempData["Success"] = "تم حفظ المستودع والرفوف بنجاح!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "حدث خطأ أثناء الحفظ: " + ex.Message;
                }
            }

            var groups = await _context.ItemGroups.Where(g => g.IsActive).Select(g => new { id = g.GroupId, name = g.GroupName }).ToListAsync();
            ViewBag.ItemGroupsJson = JsonSerializer.Serialize(groups);
            return View(warehouse);
        }

        // =======================================================
        // 3. شاشة تعديل مستودع
        // =======================================================
        [HttpGet]
        [HasPermission("Warehouses", "Edit")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var warehouse = await _context.Warehouses.Include(w => w.Shelves).FirstOrDefaultAsync(w => w.WarehouseId == id);
            if (warehouse == null) return NotFound();
            if (warehouse.BranchId != ActiveBranchId) return RedirectToAction("AccessDenied", "Home");

            var groups = await _context.ItemGroups.Where(g => g.IsActive).Select(g => new { id = g.GroupId, name = g.GroupName }).ToListAsync();
            ViewBag.ItemGroupsJson = JsonSerializer.Serialize(groups);

            return View(warehouse);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Warehouses", "Edit")]
        public async Task<IActionResult> Edit(int id, Warehouses warehouse)
        {
            if (id != warehouse.WarehouseId) return NotFound();
            ModelState.Remove("Branch");

            if (ModelState.IsValid)
            {
                // 🚀 الحل المعماري: التغليف باستراتيجية التنفيذ لحماية البيانات
                var strategy = _context.Database.CreateExecutionStrategy();
                try
                {
                    await strategy.ExecuteAsync(async () =>
                    {
                        using (var transaction = await _context.Database.BeginTransactionAsync())
                        {
                            try
                            {
                                var existingWarehouse = await _context.Warehouses.Include(w => w.Shelves).FirstOrDefaultAsync(w => w.WarehouseId == id);
                                if (existingWarehouse == null) throw new Exception("المستودع غير موجود.");

                                // تحديث البيانات الأساسية
                                existingWarehouse.WarehouseName = warehouse.WarehouseName;
                                existingWarehouse.Location = warehouse.Location;
                                existingWarehouse.IsActive = warehouse.IsActive;

                                // مسح الرفوف القديمة وإضافة الجديدة (أسهل طريقة للتحديث الدقيق)
                                _context.Shelves.RemoveRange(existingWarehouse.Shelves);
                                await _context.SaveChangesAsync();

                                if (warehouse.Shelves != null)
                                {
                                    foreach (var shelf in warehouse.Shelves)
                                    {
                                        shelf.ShelfId = 0; // تصفير الـ ID لكي يُعتبر سجلاً جديداً
                                        shelf.WarehouseId = existingWarehouse.WarehouseId;
                                        _context.Shelves.Add(shelf);
                                    }
                                }

                                _context.Warehouses.Update(existingWarehouse);
                                await _context.SaveChangesAsync();
                                await transaction.CommitAsync();
                            }
                            catch (Exception)
                            {
                                await transaction.RollbackAsync();
                                throw;
                            }
                        }
                    });

                    await RecordLog("Edit", "Warehouses", $"تعديل بيانات المستودع: {warehouse.WarehouseName}");
                    TempData["Success"] = "تم تعديل المستودع والرفوف بنجاح!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "حدث خطأ أثناء التعديل: " + ex.Message;
                }
            }

            var groups = await _context.ItemGroups.Where(g => g.IsActive).Select(g => new { id = g.GroupId, name = g.GroupName }).ToListAsync();
            ViewBag.ItemGroupsJson = JsonSerializer.Serialize(groups);
            return View(warehouse);
        }

        // =======================================================
        // 4. حذف المستودع
        // =======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Warehouses", "Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.WarehouseId == id);
            if (warehouse != null && warehouse.BranchId == ActiveBranchId)
            {
                string name = warehouse.WarehouseName;
                _context.Warehouses.Remove(warehouse);
                await _context.SaveChangesAsync();

                await RecordLog("Delete", "Warehouses", $"حذف المستودع: {name}");
                TempData["Success"] = "تم حذف المستودع بنجاح.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}