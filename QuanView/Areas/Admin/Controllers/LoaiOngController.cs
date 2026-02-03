using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using QuanView.ViewModels;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace QuanView.Areas.Admin.Controllers
{
    [Area("Admin")]
    //[Authorize(Policy = "AdminPolicy")]
    public class LoaiOngController : Controller
    {
        private readonly HttpClient _httpClient;

        public LoaiOngController(IHttpClientFactory factory)
        {
            _httpClient = factory.CreateClient("MyApi");
        }

        public async Task<IActionResult> Index(string? keyword, string? trangThai, int page = 1, int pageSize = 10)
        {
            // Gọi API GetPaged kèm keyword và trạng thái nếu có
            var url = $"LoaiOng/paged?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(keyword))
                url += $"&keyword={Uri.EscapeDataString(keyword)}";
            if (!string.IsNullOrEmpty(trangThai))
                url += $"&trangThai={Uri.EscapeDataString(trangThai)}";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Không thể tải danh sách loại ống.";
                return View(new List<LoaiOng>());
            }

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var result = JsonSerializer.Deserialize<PagedResult<LoaiOng>>(json, options);

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
            return PartialView("_CreatePartial", new LoaiOng());
        }

        [HttpPost]
        public async Task<IActionResult> Create(LoaiOng model)
        {
            Console.WriteLine("📥 MVC nhận Create từ View:");
            Console.WriteLine($"MaLoaiOng: {model.MaLoaiOng}, TenLoaiOng: {model.TenLoaiOng}, TrangThai: {model.TrangThai}");

            model.NgayTao = DateTime.Now;
            model.NguoiTao = User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

            var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("LoaiOng/Create", content);

            Console.WriteLine($"📤 Gửi API xong, Status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Lỗi từ API: {error}");
                return Json(new { success = false, message = "Không thêm được loại ống" });
            }

            TempData["message"] = "Tạo loại ống thành công";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var lo = await _httpClient.GetFromJsonAsync<LoaiOng>($"LoaiOng/{id}");
            return PartialView("_EditPartial", lo);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(LoaiOng model)
        {
            model.LanCapNhatCuoi = DateTime.Now;
            model.NguoiCapNhat = User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

            var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"LoaiOng/{model.IDLoaiOng}", content);

            return Json(new { success = response.IsSuccessStatusCode });
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var lo = await _httpClient.GetFromJsonAsync<LoaiOng>($"LoaiOng/{id}");
            return PartialView("_DetailsPartial", lo);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var response = await _httpClient.DeleteAsync($"LoaiOng/{id}");
            TempData["message"] = "Xóa loại ống thành công";
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
            var response = await _httpClient.PutAsync($"LoaiOng/ToggleStatus/{req.Id}", null);
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
