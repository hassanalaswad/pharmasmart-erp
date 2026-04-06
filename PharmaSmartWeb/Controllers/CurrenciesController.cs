using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Models;
using PharmaSmartWeb.Filters;
using System.Linq;
using System.Threading.Tasks;

namespace PharmaSmartWeb.Controllers
{
    [Authorize]
    public class CurrenciesController : BaseController
    {
        public CurrenciesController(ApplicationDbContext context) : base(context) { }

        // ==========================================
        // 📊 1. عرض قائمة العملات
        // ==========================================
        [HttpGet]
        [HasPermission("Currencies", "View")]
        public async Task<IActionResult> Index()
        {
            var currencies = await _context.Currencies.OrderByDescending(c => c.IsBaseCurrency).ThenBy(c => c.CurrencyCode).ToListAsync();
            return View(currencies);
        }

        // ==========================================
        // ➕ 2. شاشة إضافة عملة جديدة
        // ==========================================
        [HttpGet]
        [HasPermission("Currencies", "Add")]
        public IActionResult Create()
        {
            // قيم افتراضية منطقية
            return View(new Currencies { IsActive = true, ExchangeRate = 1.0000m });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Currencies", "Add")]
        public async Task<IActionResult> Create(Currencies model)
        {
            ModelState.Remove("Branches");

            if (ModelState.IsValid)
            {
                // إذا تم تحديدها كعملة أساسية، نجعل معاملها 1 ونلغي الأساسية القديمة
                if (model.IsBaseCurrency)
                {
                    var existingBase = await _context.Currencies.Where(c => c.IsBaseCurrency).ToListAsync();
                    foreach (var e in existingBase) e.IsBaseCurrency = false;
                    model.ExchangeRate = 1.0000m;
                }

                _context.Currencies.Add(model);
                await _context.SaveChangesAsync();

                await RecordLog("Add", "Currencies", $"تمت إضافة عملة جديدة: {model.CurrencyName} ({model.CurrencyCode})");
                TempData["Success"] = "تم حفظ العملة بنجاح.";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // ==========================================
        // 📝 3. شاشة تعديل عملة
        // ==========================================
        [HttpGet]
        [HasPermission("Currencies", "Edit")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var currency = await _context.Currencies.FindAsync(id);
            if (currency == null) return NotFound();
            return View(currency);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Currencies", "Edit")]
        public async Task<IActionResult> Edit(int id, Currencies model)
        {
            if (id != model.CurrencyId) return NotFound();
            ModelState.Remove("Branches");

            if (ModelState.IsValid)
            {
                if (model.IsBaseCurrency)
                {
                    var existingBase = await _context.Currencies.Where(c => c.CurrencyId != id && c.IsBaseCurrency).ToListAsync();
                    foreach (var e in existingBase) e.IsBaseCurrency = false;
                    model.ExchangeRate = 1.0000m;
                }

                _context.Update(model);
                await _context.SaveChangesAsync();

                await RecordLog("Edit", "Currencies", $"تم تعديل العملة: {model.CurrencyName}");
                TempData["Success"] = "تم تحديث بيانات العملة بنجاح.";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }
    }
}