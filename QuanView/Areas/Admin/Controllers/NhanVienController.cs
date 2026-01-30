using Microsoft.AspNetCore.Mvc;
using QuanApi.Dtos;
using QuanApi.Services;
using System.Security.Claims;

namespace QuanView.Controllers
{
    public class NhanVienController : Controller
    {
        private readonly INhanVienService _service;
        private readonly ILogger<NhanVienController> _logger;

        public NhanVienController(INhanVienService service, ILogger<NhanVienController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // GET: /NhanVien
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] NhanVienFilterDto filter)
        {
            var result = await _service.GetPagedEmployeesAsync(filter);
            return View(result); // Returns Index.cshtml with the paged list
        }

        // GET: /NhanVien/Details/{id}
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();

            return View(result);
        }

        // GET: /NhanVien/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /NhanVien/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NhanVienCreateDto dto)
        {
            if (!ModelState.IsValid) return View(dto);

            try
            {
                await _service.CreateAsync(dto);
                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(dto);
            }
        }

        // GET: /NhanVien/Edit/{id}
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();

            // Note: You might need to map 'result' to 'NhanVienUpdateDto' here
            return View(result);
        }

        // POST: /NhanVien/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, NhanVienUpdateDto dto)
        {
            if (!ModelState.IsValid) return View(dto);

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                await _service.UpdateAsync(id, dto, currentUserId);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(dto);
            }
        }

        // GET: /NhanVien/Delete/{id}
        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();

            return View(result);
        }

        // POST: /NhanVien/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return RedirectToAction(nameof(Index));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }
    }
}