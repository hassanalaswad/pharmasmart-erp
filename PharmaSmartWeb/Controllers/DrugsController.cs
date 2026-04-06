using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Models;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using PharmaSmartWeb.Filters;

namespace PharmaSmartWeb.Controllers
{
    [Authorize]
    public class DrugsController : BaseController
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public DrugsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment) : base(context)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        // =======================================================
        // 1. عرض قائمة الأدوية (Index)
        // =======================================================
        [HasPermission("Drugs", "View")]
        public async Task<IActionResult> Index()
        {
            // جلب البيانات مع المجموعة العلاجية وعرض الأحدث أولاً
            var drugs = await _context.Drugs
                .Include(d => d.ItemGroup)
                .OrderByDescending(d => d.DrugId)
                .ToListAsync();

            return View(drugs);
        }

        // =======================================================
        // 2. إضافة صنف جديد (Create)
        // =======================================================
        [HttpGet]
        [HasPermission("Drugs", "Add")]
        public IActionResult Create(bool isPopup = false) // 🚀 تم إضافة isPopup
        {
            ViewBag.IsPopup = isPopup; // تمرير الحالة للـ View
            PrepareDropdowns();
            return View(new Drugs { IsActive = true, ConversionFactor = 1 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Drugs", "Add")]
        public async Task<IActionResult> Create(Drugs newDrug, IFormFile drugImage, bool isPopup = false) // 🚀 تم إضافة isPopup هنا أيضاً
        {
            ModelState.Remove("ImagePath");
            ModelState.Remove("Barcode"); // استثناء الباركود للسماح بالتوليد الآلي
            ModelState.Remove("drugImage");
            ModelState.Remove("SaremaCategory");
            ModelState.Remove("ItemGroup");

            if (ModelState.IsValid)
            {
                try
                {
                    // 🧠 الذكاء الخارق: التكويد الآلي (Auto SKU Generation)
                    if (string.IsNullOrWhiteSpace(newDrug.Barcode))
                    {
                        if (newDrug.GroupId.HasValue)
                        {
                            var group = await _context.ItemGroups.FindAsync(newDrug.GroupId.Value);
                            string groupCode = string.IsNullOrWhiteSpace(group?.GroupCode) ? "ITM" : group.GroupCode.ToUpper();

                            int countInGroup = await _context.Drugs.CountAsync(d => d.GroupId == newDrug.GroupId.Value);
                            newDrug.Barcode = $"{groupCode}-{(countInGroup + 1).ToString("D4")}"; // مثال: PAR-0001
                        }
                        else
                        {
                            int totalCount = await _context.Drugs.CountAsync();
                            newDrug.Barcode = $"GEN-{(totalCount + 1).ToString("D5")}"; // مثال: GEN-00001
                        }
                    }
                    else
                    {
                        // فحص منع التكرار في حال الإدخال اليدوي
                        if (await _context.Drugs.AnyAsync(d => d.Barcode == newDrug.Barcode))
                        {
                            ViewBag.Error = "عفواً، الباركود المُدخل مسجل مسبقاً لصنف آخر.";
                            ViewBag.IsPopup = isPopup;
                            PrepareDropdowns(newDrug.GroupId);
                            return View(newDrug);
                        }
                    }

                    // معالجة الصورة
                    if (drugImage != null && drugImage.Length > 0)
                    {
                        if (!PharmaSmartWeb.Security.FileSecurityHelper.IsValidImageFile(drugImage))
                        {
                            ViewBag.Error = "الصورة غير صالحة أو امتداد غير مسموح.";
                            ViewBag.IsPopup = isPopup;
                            PrepareDropdowns(newDrug.GroupId);
                            return View(newDrug);
                        }

                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "drugs");
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                        string uniqueFileName = PharmaSmartWeb.Security.FileSecurityHelper.SanitizeFileName(drugImage.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await drugImage.CopyToAsync(fileStream);
                        }

                        newDrug.ImagePath = uniqueFileName;
                    }

                    newDrug.CreatedAt = DateTime.Now;
                    newDrug.CreatedBy = int.Parse(User.FindFirst("UserID")?.Value ?? "0");

                    _context.Drugs.Add(newDrug);
                    await _context.SaveChangesAsync();

                    await RecordLog("Add", "Drugs", $"إضافة دواء جديد: {newDrug.DrugName} بكود {newDrug.Barcode}");

                    // 🚀 السحر البرمجي: إذا كانت نافذة منبثقة، نغلقها ونحدث شاشة المشتريات تلقائياً
                    if (isPopup)
                    {
                        return Content("<html><body><script>window.parent.postMessage('drug_added_success', '*');</script></body></html>", "text/html");
                    }

                    TempData["Success"] = $"تم إضافة الصنف بنجاح! الرمز المعتمد هو: {newDrug.Barcode}";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "حدث خطأ غير متوقع: " + ex.Message;
                    ViewBag.IsPopup = isPopup;
                    PrepareDropdowns(newDrug.GroupId);
                    return View(newDrug);
                }
            }

            ViewBag.Error = "لم يتم إضافة الدواء! يرجى التأكد من تعبئة الحقول الإلزامية.";
            ViewBag.IsPopup = isPopup;
            PrepareDropdowns(newDrug.GroupId);
            return View(newDrug);
        }

        // =======================================================
        // 3. تعديل صنف موجود (Edit)
        // =======================================================
        [HttpGet]
        [HasPermission("Drugs", "Edit")]
        public async Task<IActionResult> Edit(int? id, bool isPopup = false)
        {
            if (id == null) return NotFound();

            var drug = await _context.Drugs.FindAsync(id);
            if (drug == null) return NotFound();

            ViewBag.IsPopup = isPopup;
            PrepareDropdowns(drug.GroupId);
            return View(drug);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Drugs", "Edit")]
        public async Task<IActionResult> Edit(int id, Drugs updatedDrug, IFormFile drugImage, bool isPopup = false)
        {
            if (id != updatedDrug.DrugId) return NotFound();

            ModelState.Remove("ImagePath");
            ModelState.Remove("drugImage");
            ModelState.Remove("Barcode");
            ModelState.Remove("SaremaCategory");
            ModelState.Remove("ItemGroup");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingDrug = await _context.Drugs.FirstOrDefaultAsync(d => d.DrugId == id);
                    if (existingDrug == null) return NotFound();

                    // التأكد من الباركود
                    if (!string.IsNullOrWhiteSpace(updatedDrug.Barcode) && existingDrug.Barcode != updatedDrug.Barcode)
                    {
                        if (await _context.Drugs.AnyAsync(d => d.Barcode == updatedDrug.Barcode))
                        {
                            ViewBag.Error = "عفواً، الباركود الجديد مسجل مسبقاً.";
                            PrepareDropdowns(updatedDrug.GroupId);
                            return View(updatedDrug);
                        }
                        existingDrug.Barcode = updatedDrug.Barcode;
                    }

                    existingDrug.DrugName = updatedDrug.DrugName;
                    existingDrug.Manufacturer = updatedDrug.Manufacturer;
                    existingDrug.IsActive = updatedDrug.IsActive;
                    existingDrug.UpdatedAt = DateTime.Now;
                    existingDrug.UpdatedBy = int.Parse(User.FindFirst("UserID")?.Value ?? "0");

                    existingDrug.GroupId = updatedDrug.GroupId;
                    existingDrug.MainUnit = updatedDrug.MainUnit;
                    existingDrug.SubUnit = updatedDrug.SubUnit;
                    existingDrug.ConversionFactor = updatedDrug.ConversionFactor;

                    if (drugImage != null && drugImage.Length > 0)
                    {
                        if (!PharmaSmartWeb.Security.FileSecurityHelper.IsValidImageFile(drugImage))
                        {
                            ViewBag.Error = "الصورة غير صالحة أو امتداد غير مسموح.";
                            ViewBag.IsPopup = isPopup;
                            PrepareDropdowns(updatedDrug.GroupId);
                            return View(updatedDrug);
                        }

                        if (!string.IsNullOrEmpty(existingDrug.ImagePath))
                        {
                            string oldPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "drugs", existingDrug.ImagePath);
                            if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                        }

                        string uniqueFileName = PharmaSmartWeb.Security.FileSecurityHelper.SanitizeFileName(drugImage.FileName);
                        string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "drugs", uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await drugImage.CopyToAsync(fileStream);
                        }
                        existingDrug.ImagePath = uniqueFileName;
                    }

                    _context.Update(existingDrug);
                    await _context.SaveChangesAsync();

                    await RecordLog("Edit", "Drugs", $"تعديل بيانات الصنف (Master Data): {updatedDrug.DrugName}");

                    if (isPopup)
                    {
                        return Content("<html><body><script>window.parent.postMessage('drug_updated_success', '*');</script></body></html>", "text/html");
                    }

                    TempData["Success"] = "تم تحديث بيانات الصنف الأساسية بنجاح!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    ViewBag.Error = "فشل التحديث! تأكد من صحة البيانات.";
                    ViewBag.IsPopup = isPopup;
                    PrepareDropdowns(updatedDrug.GroupId);
                    return View(updatedDrug);
                }
            }

            ViewBag.IsPopup = isPopup;
            PrepareDropdowns(updatedDrug.GroupId);
            return View(updatedDrug);
        }

        // =======================================================
        // 4. حذف صنف (Delete)
        // =======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Drugs", "Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            var drug = await _context.Drugs.FindAsync(id);
            if (drug != null)
            {
                string drugName = drug.DrugName;

                if (!string.IsNullOrEmpty(drug.ImagePath))
                {
                    string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "drugs", drug.ImagePath);
                    if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
                }

                _context.Drugs.Remove(drug);
                await _context.SaveChangesAsync();

                await RecordLog("Delete", "Drugs", $"تم حذف الدواء نهائياً من النظام المركزي: {drugName}");
            }

            return RedirectToAction(nameof(Index));
        }

        private void PrepareDropdowns(int? selectedGroupId = null)
        {
            ViewBag.Groups = new SelectList(_context.ItemGroups.OrderBy(g => g.GroupName), "GroupId", "GroupName", selectedGroupId);
        }
    }
}