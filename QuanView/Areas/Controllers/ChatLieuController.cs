using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using QuanView.Services;
using System.Security.Claims;

[Area("Admin")]
[Authorize(Policy = "AdminPolicy")]
public class ChatLieuController : Controller
{
    private readonly IChatLieuService _service;

    public ChatLieuController(IChatLieuService service)
    {
        _service = service;
    }

    public async Task<IActionResult> Index(
        string? keyword,
        string? trangThai,
        int page = 1,
        int pageSize = 10)
    {
        try
        {
            var result = await _service.GetPagedAsync(
                keyword,
                trangThai,
                page,
                pageSize);

            // Gửi các biến ra view để làm phân trang và giữ lại giá trị lọc
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
            return View(new List<ChatLieu>());
        }
    }
    public async Task<IActionResult> Create()
    {
        return PartialView("_CreatePartial", new ChatLieu());
    }

    // POST: Create
    [HttpPost]
    public async Task<IActionResult> Create(ChatLieu model)
    {
        var nguoiTao = User?.FindFirst(ClaimTypes.Name)?.Value;

        var success = await _service.CreateAsync(model, nguoiTao);

        if (!success)
        {
            return Json(new { success = false, message = "Không thêm được chất liệu" });
        }

        TempData["message"] = "Tạo chất liệu thành công";
        return RedirectToAction("Index");
    }

    // GET: Edit
    public async Task<IActionResult> Edit(Guid id)
    {
        var cl = await _service.GetByIdAsync(id);
        return PartialView("_EditPartial", cl);
    }
    // POST: Edit
    [HttpPost]
    public async Task<IActionResult> Edit(ChatLieu model)
    {
        var nguoiCapNhat = User?.FindFirst(ClaimTypes.Name)?.Value;

        var success = await _service.UpdateAsync(model, nguoiCapNhat);
        return Json(new { success });
    }

    // GET: Details
    public async Task<IActionResult> Details(Guid id)
    {
        var cl = await _service.GetByIdAsync(id);
        return PartialView("_DetailsPartial", cl);
    }

    // POST: Delete
    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _service.DeleteAsync(id);

        if (success)
            TempData["message"] = "Xóa chất liệu thành công";

        return Json(new { success });
    }

    public class ToggleStatusRequest
    {
        public Guid Id { get; set; }
    }

    // POST: ToggleStatus
    [HttpPost]
    public async Task<IActionResult> ToggleStatus([FromBody] ToggleStatusRequest req)
    {
        var (success, trangThai) = await _service.ToggleStatusAsync(req.Id);

        if (!success)
            return Json(new { success = false });

        return Json(new { success = true, trangThai });
    }
}
