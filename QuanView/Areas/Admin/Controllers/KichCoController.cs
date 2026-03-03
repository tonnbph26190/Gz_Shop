using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using QuanView.ViewModels;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace QuanView.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class KichCoController : Controller
    {
        private readonly HttpClient _httpClient;

        public KichCoController(IHttpClientFactory factory)
        {
            _httpClient = factory.CreateClient("MyApi");
        }

        public async Task<IActionResult> Index(string? keyword, string? trangThai, int page = 1, int pageSize = 10)
        {
            // Gọi API GetPaged kèm keyword và trangThai nếu có
            var url = $"KichCo/paged?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(keyword))
                url += $"&keyword={Uri.EscapeDataString(keyword)}";
            if (!string.IsNullOrEmpty(trangThai))
                url += $"&trangThai={Uri.EscapeDataString(trangThai)}";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Không thể tải danh sách kích cỡ.";
                return View(new List<KichCo>());
            }

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var result = JsonSerializer.Deserialize<PagedResult<KichCo>>(json, options);

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = result?.Total ?? 0;
            ViewBag.Keyword = keyword ?? "";
            ViewBag.Status = trangThai ?? "";

            return View(result?.Data ?? new List<KichCo>());
        }

        public async Task<IActionResult> Create()
        {
            return PartialView("_CreatePartial", new KichCo());
        }

        [HttpPost]
        public async Task<IActionResult> Create(KichCo model)
        {
            // Validate size values
            var allowedSizes = new[] { "S", "M", "L", "XL", "XXL" };
            if (!allowedSizes.Contains(model.TenKichCo))
            {
                return Json(new { success = false, message = "Tên kích cỡ chỉ được phép là: S, M, L, XL, XXL" });
            }

            model.NgayTao = DateTime.Now;
            model.NguoiTao = User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

            var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("KichCo/Create", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return Json(new { success = false, message = "Không thêm được kích cỡ" });
            }

            TempData["message"] = "Tạo kích cỡ thành công";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var kc = await _httpClient.GetFromJsonAsync<KichCo>($"KichCo/{id}");
            return PartialView("_EditPartial", kc);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(KichCo model)
        {
            // Validate size values
            var allowedSizes = new[] { "S", "M", "L", "XL", "XXL" };
            if (!allowedSizes.Contains(model.TenKichCo))
            {
                return Json(new { success = false, message = "Tên kích cỡ chỉ được phép là: S, M, L, XL, XXL" });
            }

            model.LanCapNhatCuoi = DateTime.Now;
            model.NguoiCapNhat = User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

            var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"KichCo/{model.IDKichCo}", content);

            return Json(new { success = response.IsSuccessStatusCode });
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var kc = await _httpClient.GetFromJsonAsync<KichCo>($"KichCo/{id}");
            return PartialView("_DetailsPartial", kc);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var response = await _httpClient.DeleteAsync($"KichCo/{id}");
            TempData["message"] = "Xóa kích cỡ thành công";
            return Json(new { success = response.IsSuccessStatusCode });
        }

        public class ToggleStatusRequest
        {
            public Guid Id { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus([FromBody] ToggleStatusRequest req)
        {
            var response = await _httpClient.PutAsync($"KichCo/ToggleStatus/{req.Id}", null);
            if (!response.IsSuccessStatusCode)
                return Json(new { success = false });

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            var trangThai = data != null && data.TryGetValue("trangThai", out var je) ? je.GetBoolean() : (bool?)null;
            return Json(new { success = true, trangThai });
        }
    }
}