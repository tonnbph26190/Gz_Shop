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

            // Gửi các biến ra view để làm phân trang và giữ lại giá trị lọc
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
                return Json(new { success = false, message = "Tên kích cỡ chỉ được phép là: S, M, L, XL, XXL" });
            }

            model.NgayTao = DateTime.Now;
            model.NguoiTao = User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

            var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("KichCo/Create", content);

            Console.WriteLine($"📤 Gửi API xong, Status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Lỗi từ API: {error}");
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
            Console.WriteLine($"📥 MVC nhận ToggleStatus với ID: {req.Id}");
            var response = await _httpClient.PutAsync($"KichCo/ToggleStatus/{req.Id}", null);
            Console.WriteLine($"📤 API trả về status: {response.StatusCode}");

            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"📩 Nội dung trả về: {json}");

            if (!response.IsSuccessStatusCode)
                return Json(new { success = false });

            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            return Json(new { success = true, trangThai = data["trangThai"] });
        }
    }
} 