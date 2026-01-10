using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using QuanView.Areas.Admin.Models;
using System.Text;
using QuanApi.Dtos;
using System.Linq;

namespace QuanView.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class AnhSanPhamController : Controller
    {
        private readonly HttpClient _http;

        public AnhSanPhamController(IHttpClientFactory factory)
        {
            _http = factory.CreateClient("MyApi");
        }

        // GET: Lấy danh sách ảnh của sản phẩm chi tiết
        [HttpGet]
        public async Task<IActionResult> GetImages(Guid sanPhamChiTietId)
        {
            try
            {
                var response = await _http.GetAsync($"sanphams/chitiet/{sanPhamChiTietId}/images");
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, message = $"Không thể tải danh sách ảnh: {response.StatusCode} - {errorContent}" });
                }

                var json = await response.Content.ReadAsStringAsync();
                
                // Kiểm tra xem JSON có hợp lệ không
                if (string.IsNullOrWhiteSpace(json))
                {
                    return Json(new { success = false, message = "API trả về dữ liệu rỗng" });
                }

                var apiImages = JsonSerializer.Deserialize<List<QuanApi.Dtos.AnhSanPhamDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var images = MapApiImagesToAdminImages(apiImages);

                return Json(new { success = true, data = images });
            }
            catch (JsonException ex)
            {
                return Json(new { success = false, message = $"Lỗi parse JSON: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi không xác định: {ex.Message}" });
            }
        }

        // POST: Thêm ảnh mới
        [HttpPost]
        public async Task<IActionResult> AddImage(Guid sanPhamChiTietId, [FromBody] QuanView.Areas.Admin.Models.AddAnhSanPhamDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
                }

                // Map từ Admin DTO sang API DTO
                var apiDto = new QuanApi.Dtos.AddAnhSanPhamDto
                {
                    UrlAnh = dto.UrlAnh,
                    LaAnhChinh = dto.LaAnhChinh
                };
                
                var response = await _http.PostAsJsonAsync($"sanphams/chitiet/{sanPhamChiTietId}/images", apiDto);
                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, message = $"Lỗi: {response.StatusCode} - {errorMessage}" });
                }

                var result = await response.Content.ReadFromJsonAsync<object>();
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi không xác định: {ex.Message}" });
            }
        }

        // DELETE: Xóa ảnh
        [HttpDelete]
        public async Task<IActionResult> DeleteImage(Guid imageId)
        {
            try
            {
                var response = await _http.DeleteAsync($"sanphams/images/{imageId}");
                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, message = $"Lỗi: {response.StatusCode} - {errorMessage}" });
                }

                return Json(new { success = true, message = "Đã xóa ảnh thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi không xác định: {ex.Message}" });
            }
        }

        // PUT: Đặt ảnh làm ảnh chính
        [HttpPut]
        public async Task<IActionResult> SetMainImage(Guid imageId)
        {
            try
            {
                var response = await _http.PutAsync($"sanphams/images/{imageId}/set-main", null);
                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, message = $"Lỗi: {response.StatusCode} - {errorMessage}" });
                }

                return Json(new { success = true, message = "Đã đặt ảnh làm ảnh chính" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi không xác định: {ex.Message}" });
            }
        }

        // Helper method để map từ API DTO sang Admin DTO
        private List<QuanView.Areas.Admin.Models.AnhSanPhamDto> MapApiImagesToAdminImages(List<QuanApi.Dtos.AnhSanPhamDto> apiImages)
        {
            return apiImages?.Select(img => new QuanView.Areas.Admin.Models.AnhSanPhamDto
            {
                IDAnhSanPham = img.IDAnhSanPham,
                MaAnh = img.MaAnh,
                IDSanPhamChiTiet = img.IDSanPhamChiTiet,
                UrlAnh = img.UrlAnh,
                LaAnhChinh = img.LaAnhChinh,
                NgayTao = img.NgayTao,
                NguoiTao = img.NguoiTao,
                LanCapNhatCuoi = img.LanCapNhatCuoi,
                NguoiCapNhat = img.NguoiCapNhat,
                TrangThai = img.TrangThai
            }).ToList() ?? new List<QuanView.Areas.Admin.Models.AnhSanPhamDto>();
        }
    }
} 