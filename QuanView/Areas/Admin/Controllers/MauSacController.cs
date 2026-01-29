using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using QuanApi.Services;

namespace QuanView.Controllers
{
    // Created in the QuanView project
    public class MauSacController : Controller
    {
        private readonly IMauSacService _mauSacService;

        public MauSacController(IMauSacService mauSacService)
        {
            _mauSacService = mauSacService;
        }

        // GET: MauSac
        [HttpGet]
        public async Task<IActionResult> Index(string? keyword)
        {
            var data = await _mauSacService.GetAllAsync(keyword);
            return View(data); // Right-click -> Add View (Template: List)
        }

        // GET: MauSac/Details/id
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var result = await _mauSacService.GetByIdAsync(id);
            if (result == null) return NotFound();
            return View(result); // Right-click -> Add View (Template: Details)
        }

        // GET: MauSac/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(); // Right-click -> Add View (Template: Create)
        }

        // POST: MauSac/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MauSac ms)
        {
            if (ModelState.IsValid)
            {
                var success = await _mauSacService.CreateAsync(ms);
                if (success) return RedirectToAction(nameof(Index));
            }
            return View(ms);
        }

        // GET: MauSac/Edit/id
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var result = await _mauSacService.GetByIdAsync(id);
            if (result == null) return NotFound();
            return View(result); // Right-click -> Add View (Template: Edit)
        }

        // POST: MauSac/Edit/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, MauSac ms)
        {
            if (ModelState.IsValid)
            {
                var success = await _mauSacService.UpdateAsync(id, ms);
                if (success) return RedirectToAction(nameof(Index));
            }
            return View(ms);
        }

        // GET: MauSac/Delete/id
        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _mauSacService.GetByIdAsync(id);
            if (result == null) return NotFound();
            return View(result); // Right-click -> Add View (Template: Delete)
        }

        // POST: MauSac/Delete/id
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            await _mauSacService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}