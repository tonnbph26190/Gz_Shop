using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using QuanView.ViewModels;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace QuanView.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HoaTietController : Controller
    {
        private readonly HttpClient _httpClient;

        public HoaTietController(IHttpClientFactory factory)
        {
            _httpClient = factory.CreateClient("MyApi");
        }

        public async Task<IActionResult> Index(string? keyword, string? trangThai, int page = 1, int pageSize = 10)
        {
            // Gọi API GetPaged kèm keyword và trangThai nếu có
            var url = $"HoaTiet/paged?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(keyword))
                url += $"&keyword={Uri.EscapeDataString(keyword)}";
            if (!string.IsNullOrEmpty(trangThai))
                url += $"&trangThai={Uri.EscapeDataString(trangThai)}";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Không thể tải danh sách họa tiết.";
                return View(new List<HoaTiet>());
            }

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var result = JsonSerializer.Deserialize<PagedResult<HoaTiet>>(json, options);

            // Gửi các biến ra view để làm phân trang và giữ lại giá trị lọc
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = result?.Total ?? 0;
            ViewBag.Keyword = keyword ?? "";
            ViewBag.Status = trangThai ?? "";

            return View(result?.Data ?? new List<HoaTiet>());
        }

        public async Task<IActionResult> Create()
        {
            return PartialView("Create", new HoaTiet());
        }

        [HttpPost]
        public async Task<IActionResult> Create(HoaTiet model)
        {
            model.NgayTao = DateTime.Now;
            model.NguoiTao = User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

            var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("HoaTiet/Create", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return Json(new { success = false, message = "Không thêm được họa tiết" });
            }

            TempData["message"] = "Tạo họa tiết thành công";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var ht = await _httpClient.GetFromJsonAsync<HoaTiet>($"HoaTiet/{id}");
            return PartialView("Edit", ht);
        }   

        [HttpPost]
       
        public async Task<IActionResult> Edit(HoaTiet model)
        {
            model.LanCapNhatCuoi = DateTime.Now;
            model.NguoiCapNhat = User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

            var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"HoaTiet/{model.IDHoaTiet}", content);

            return Json(new { success = response.IsSuccessStatusCode });
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var ht = await _httpClient.GetFromJsonAsync<HoaTiet>($"HoaTiet/{id}");
            return PartialView("Details", ht);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var response = await _httpClient.DeleteAsync($"HoaTiet/{id}");
            TempData["message"] = "Xóa họa tiết thành công";
            return Json(new { success = response.IsSuccessStatusCode });
        }

        public class ToggleStatusRequest
        {
            public Guid Id { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus([FromBody] ToggleStatusRequest req)
        {
            var response = await _httpClient.PutAsync($"HoaTiet/ToggleStatus/{req.Id}", null);
            if (!response.IsSuccessStatusCode)
                return Json(new { success = false });

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            var trangThai = data != null && data.TryGetValue("trangThai", out var je) ? je.GetBoolean() : (bool?)null;
            return Json(new { success = true, trangThai });
        }
    }
}