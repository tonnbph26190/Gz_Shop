using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using QuanView.Areas.Admin.Services;

using System.Security.Claims;

namespace QuanView.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class KichCoController : Controller
    {
        private readonly IKichCoService _kichCoService;

        public KichCoController(IKichCoService kichCoService)
        {
            _kichCoService = kichCoService;
        }

        public async Task<IActionResult> Index(string? keyword, string? trangThai, int page = 1, int pageSize = 10)
        {
            var result = await _kichCoService.GetPagedAsync(keyword, trangThai, page, pageSize);

            if (result == null)
            {
                ViewBag.Error = "Không thể tải danh sách kích cỡ.";
                return View(new List<KichCo>());
            }

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = result.Total;
            ViewBag.Keyword = keyword ?? "";
            ViewBag.Status = trangThai ?? "";

            return View(result.Data);
        }

        public async Task<IActionResult> Create()
        {
            return PartialView("_CreatePartial", new KichCo());
        }

        [HttpPost]
        public async Task<IActionResult> Create(KichCo model)
        {
            Console.WriteLine("📥 MVC nhận Create từ View:");
            Console.WriteLine($"MaKichCo: {model.MaKichCo}, TenKichCo: {model.TenKichCo}, TrangThai: {model.TrangThai}");

            // Validate size values
            var allowedSizes = new[] { "S", "M", "L", "XL", "XXL" };
            if (!allowedSizes.Contains(model.TenKichCo))
            {
                return Json(new
                {
                    success = false,
                    message = "Tên kích cỡ chỉ được phép là: S, M, L, XL, XXL"
                });
            }

            var nguoiTao = User?.FindFirst(ClaimTypes.Name)?.Value;
            var success = await _kichCoService.CreateAsync(model, nguoiTao);

            if (!success)
            {
                return Json(new
                {
                    success = false,
                    message = "Không thêm được kích cỡ"
                });
            }

            TempData["message"] = "Tạo kích cỡ thành công";
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> Edit(Guid id)
        {
            var kc = await _kichCoService.GetByIdAsync(id);
            return PartialView("_EditPartial", kc);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(KichCo model)
        {
            // Validate size values
            var allowedSizes = new[] { "S", "M", "L", "XL", "XXL" };
            if (!allowedSizes.Contains(model.TenKichCo))
            {
                return Json(new
                {
                    success = false,
                    message = "Tên kích cỡ chỉ được phép là: S, M, L, XL, XXL"
                });
            }

            var nguoiCapNhat = User?.FindFirst(ClaimTypes.Name)?.Value;
            var success = await _kichCoService.UpdateAsync(model, nguoiCapNhat);

            return Json(new { success });
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var kc = await _kichCoService.GetByIdAsync(id);
            return PartialView("_DetailsPartial", kc);
        }
        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _kichCoService.DeleteAsync(id);

            TempData["message"] = "Xóa kích cỡ thành công";
            return Json(new { success });
        }

        public class ToggleStatusRequest
        {
            public Guid Id { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus([FromBody] ToggleStatusRequest req)
        {
            Console.WriteLine($"📥 MVC nhận ToggleStatus với ID: {req.Id}");

            var (success, trangThai) = await _kichCoService.ToggleStatusAsync(req.Id);

            if (!success)
                return Json(new { success = false });

            return Json(new { success = true, trangThai });
        }
    }
}
