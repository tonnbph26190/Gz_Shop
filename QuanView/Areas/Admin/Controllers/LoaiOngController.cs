using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using QuanApi.Services;

namespace QuanView.Controllers
{
    // Inheriting from Controller enables the "Add View" right-click feature
    public class LoaiOngController : Controller
    {
        private readonly ILoaiOngService _service;

        public LoaiOngController(ILoaiOngService service)
        {
            _service = service;
        }

        // GET: LoaiOng
        [HttpGet]
        public async Task<IActionResult> Index(string? keyword)
        {
            var data = await _service.GetAllAsync(keyword);
            return View(data); // Right-click -> Add View (Template: List)
        }

        // GET: LoaiOng/Details/{id}
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return View(result); // Right-click -> Add View (Template: Details)
        }

        // GET: LoaiOng/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(); // Right-click -> Add View (Template: Create)
        }

        // POST: LoaiOng/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LoaiOng lo)
        {
            if (ModelState.IsValid)
            {
                var success = await _service.CreateAsync(lo);
                if (success) return RedirectToAction(nameof(Index));
            }
            return View(lo);
        }

        // GET: LoaiOng/Edit/{id}
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return View(result); // Right-click -> Add View (Template: Edit)
        }

        // POST: LoaiOng/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, LoaiOng lo)
        {
            if (ModelState.IsValid)
            {
                var success = await _service.UpdateAsync(id, lo);
                if (success) return RedirectToAction(nameof(Index));
            }
            return View(lo);
        }

        // GET: LoaiOng/Delete/{id}
        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return View(result); // Right-click -> Add View (Template: Delete)
        }

        // POST: LoaiOng/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            await _service.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // Optional: Action for ToggleStatus if you want to trigger it from the UI
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            await _service.ToggleStatusAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}