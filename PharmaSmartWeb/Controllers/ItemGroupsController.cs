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
    public class ItemGroupsController : BaseController
    {
        public ItemGroupsController(ApplicationDbContext context) : base(context) { }

        [HttpGet]
        [HasPermission("ItemGroups", "View")]
        public async Task<IActionResult> Index()
        {
            var groups = await _context.ItemGroups
                .Include(g => g.Drugs) // لمعرفة عدد الأدوية المربوطة
                .OrderBy(g => g.GroupCode)
                .ToListAsync();
            return View(groups);
        }

        [HttpGet]
        [HasPermission("ItemGroups", "Add")]
        public IActionResult Create()
        {
            return View(new ItemGroups { IsActive = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("ItemGroups", "Add")]
        public async Task<IActionResult> Create(ItemGroups itemGroup)
        {
            if (ModelState.IsValid)
            {
                _context.ItemGroups.Add(itemGroup);
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم حفظ المجموعة العلاجية بنجاح.";
                return RedirectToAction(nameof(Index));
            }
            return View(itemGroup);
        }

        [HttpGet]
        [HasPermission("ItemGroups", "Edit")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var itemGroup = await _context.ItemGroups.FindAsync(id);
            if (itemGroup == null) return NotFound();
            return View(itemGroup);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("ItemGroups", "Edit")]
        public async Task<IActionResult> Edit(int id, ItemGroups itemGroup)
        {
            if (id != itemGroup.GroupId) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(itemGroup);
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم تحديث المجموعة بنجاح.";
                return RedirectToAction(nameof(Index));
            }
            return View(itemGroup);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("ItemGroups", "Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            var group = await _context.ItemGroups.Include(g => g.Drugs).FirstOrDefaultAsync(g => g.GroupId == id);
            if (group != null)
            {
                if (group.Drugs.Any())
                {
                    TempData["Error"] = "لا يمكن حذف هذه المجموعة لارتباط أدوية بها.";
                    return RedirectToAction(nameof(Index));
                }
                _context.ItemGroups.Remove(group);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}