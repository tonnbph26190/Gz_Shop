using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using QuanApi.Services;

namespace QuanView.Controllers
{
    // Note: No [ApiController] and we inherit from Controller
    public class LoaiOngController : Controller
    {
        private readonly ILoaiOngService _service;

        public LoaiOngController(ILoaiOngService service)
        {
            _service = service;
        }

        // 1. List View (GET All)
        [HttpGet]
        public async Task<IActionResult> Index(string? keyword)
        {
            var data = await _service.GetAllAsync(keyword);
            return View(data); // Right-click 'View' to add Index.cshtml
        }

        // 2. Details View
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return View(result);
        }

        // 3. Create (GET - Show the form)
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // 4. Create (POST - Save the data)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LoaiOng lo)
        {
            if (ModelState.IsValid)
            {
                await _service.CreateAsync(lo);
                return RedirectToAction(nameof(Index));
            }
            return View(lo);
        }

        // 5. Edit (GET - Show form with existing data)
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return View(result);
        }

        // 6. Edit (POST - Save changes)
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

        // 7. Delete (GET - Confirm delete page)
        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return View(result);
        }

        // 8. Delete (POST - Confirmed)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            await _service.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}