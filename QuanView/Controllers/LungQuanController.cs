using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using QuanApi.Services;

namespace QuanView.Controllers
{
    // Inheriting from Controller enables the "Add View" right-click feature
    public class LungQuanController : Controller
    {
        private readonly ILungQuanService _service;

        public LungQuanController(ILungQuanService service)
        {
            _service = service;
        }

        // GET: LungQuan (The List View)
        [HttpGet]
        public async Task<IActionResult> Index(string? keyword)
        {
            var data = await _service.GetAllAsync(keyword);
            return View(data); // Right-click here -> Add View
        }

        // GET: LungQuan/Details/{id}
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        // GET: LungQuan/Create (Show empty form)
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: LungQuan/Create (Save form data)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LungQuan lq)
        {
            if (ModelState.IsValid)
            {
                await _service.CreateAsync(lq);
                return RedirectToAction(nameof(Index));
            }
            return View(lq);
        }

        // GET: LungQuan/Edit/{id} (Show form with data)
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        // POST: LungQuan/Edit/{id} (Update data)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, LungQuan lq)
        {
            if (ModelState.IsValid)
            {
                var success = await _service.UpdateAsync(id, lq);
                if (success) return RedirectToAction(nameof(Index));
            }
            return View(lq);
        }

        // GET: LungQuan/Delete/{id} (Confirm delete page)
        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        // POST: LungQuan/Delete/{id} (Actually perform delete)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            await _service.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}