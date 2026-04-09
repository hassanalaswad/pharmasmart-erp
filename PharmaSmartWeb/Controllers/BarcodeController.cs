using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaSmartWeb.Models;
using PharmaSmartWeb.Filters;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PharmaSmartWeb.Controllers
{
    [Authorize]
    public class BarcodeController : BaseController
    {
        public BarcodeController(ApplicationDbContext context) : base(context) { }

        [HttpGet]
        [HasPermission("Inventory", "View")]
        public async Task<IActionResult> Index()
        {
            // ?? ÇáÚÒá: ÚÑÖ ÇáÃÏæíÉ ÇáãÚáÞÉ ááØÈÇÚÉ Ýí ÇáÝÑÚ ÇáäÔØ ÝÞØ
            var pendingBarcodes = await _context.BarcodeGenerator
                .Include(b => b.Drug)
                .Where(b => b.IsPrinted == false && b.BranchId == ActiveBranchId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(pendingBarcodes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PrintSelected(List<int> selectedIds, Dictionary<int, int> printQuantities)
        {
            if (selectedIds == null || !selectedIds.Any())
            {
                TempData["Error"] = "íÑÌì ÊÍÏíÏ ÕäÝ æÇÍÏ Úáì ÇáÃÞá ááØÈÇÚÉ.";
                return RedirectToAction(nameof(Index));
            }

            // ?? ÇáÚÒá: ÇáÊÃßÏ ãä Ãä ÇáÈÇÑßæÏÇÊ ÊÊÈÚ ÇáÝÑÚ ÇáäÔØ ÞÈá ÇáØÈÇÚÉ
            var barcodesToPrint = await _context.BarcodeGenerator
                .Include(b => b.Drug)
                .Where(b => selectedIds.Contains(b.Id) && b.BranchId == ActiveBranchId)
                .ToListAsync();

            foreach (var item in barcodesToPrint)
            {
                if (printQuantities.ContainsKey(item.Id))
                {
                    item.QuantityToPrint = printQuantities[item.Id];
                }
            }

            await _context.SaveChangesAsync();
            return View("Print", barcodesToPrint);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsPrinted([FromBody] List<int> printedIds)
        {
            if (printedIds == null || !printedIds.Any()) return Json(new { success = false });

            // ?? ÇáÚÒá: ÅÞÝÇá ÇáÈÇÑßæÏÇÊ ÇáãØÈæÚÉ Ýí åÐÇ ÇáÝÑÚ ÝÞØ
            var barcodes = await _context.BarcodeGenerator
                .Where(b => printedIds.Contains(b.Id) && b.BranchId == ActiveBranchId)
                .ToListAsync();

            foreach (var item in barcodes)
            {
                item.IsPrinted = true;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            // ?? ÇáÚÒá: ÇáÍÐÝ áÇ íÊã ÅáÇ ÅÐÇ ßÇä ÇáÈÇÑßæÏ íäÊãí ááÝÑÚ ÇáäÔØ
            var item = await _context.BarcodeGenerator.FirstOrDefaultAsync(b => b.Id == id && b.BranchId == ActiveBranchId);
            if (item != null)
            {
                _context.BarcodeGenerator.Remove(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
