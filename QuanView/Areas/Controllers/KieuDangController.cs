using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using QuanView.Areas.Admin.Services;


namespace QuanView.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminPolicy")]
    public class KieuDangController : Controller
    {
        private readonly IKieuDangService _service;

        public KieuDangController(IKieuDangService service)
        {
            _service = service;
        }

        public async Task<IActionResult> Index(string? keyword, string? trangThai, int page = 1, int pageSize = 10)
        {
            try
            {
                var result = await _service.GetPagedAsync(keyword, trangThai, page, pageSize);

                ViewBag.Page = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalItems = result.Total;
                ViewBag.Keyword = keyword ?? "";
                ViewBag.Status = trangThai ?? "";

                return View(result.Data);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(new List<KieuDang>());
            }
        }

        public async Task<IActionResult> Create()
        {
            return PartialView("_CreatePartial", new KieuDang());
        }

        [HttpPost]
        public async Task<IActionResult> Create(KieuDang model)
        {
            model.NgayTao = DateTime.Now;
            model.NguoiTao = User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

            var success = await _service.CreateAsync(model);

            if (!success)
                return Json(new { success = false, message = "Không thêm được kiểu dáng" });

            TempData["message"] = "Tạo kiểu dáng thành công";
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> Edit(Guid id)
        {
            var kd = await _service.GetByIdAsync(id);
            return PartialView("_EditPartial", kd);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(KieuDang model)
        {
            model.LanCapNhatCuoi = DateTime.Now;
            model.NguoiCapNhat = User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

            var success = await _service.UpdateAsync(model);

            return Json(new { success });
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var kd = await _service.GetByIdAsync(id);
            return PartialView("_DetailsPartial", kd);
        }
        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _service.DeleteAsync(id);

            if (success)
                TempData["message"] = "Xóa kiểu dáng thành công";

            return Json(new { success });
        }

        public class ToggleStatusRequest
        {
            public Guid Id { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus([FromBody] ToggleStatusRequest req)
        {
            var (success, trangThai) = await _service.ToggleStatusAsync(req.Id);

            if (!success)
                return Json(new { success = false });

            return Json(new { success = true, trangThai });
        }
    }
}
