using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace QuanView.Areas.Admin.Controllers
{
    [Area("Admin")]
    //[Authorize]
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

		[HttpPost]
		public async Task<IActionResult> AddImage(
		Guid sanPhamChiTietId,
		[FromForm] List<IFormFile> files,
		[FromForm] bool laAnhChinh)
		{
			try
			{
				if (files == null || !files.Any())
				{
					return Json(new
					{
						success = false,
						message = "File không hợp lệ"
					});
				}

				var uploadedUrls = new List<string>();

				for (int i = 0; i < files.Count; i++)
				{
					var file = files[i];

					if (file == null || file.Length == 0)
						continue;

					var formData = new MultipartFormDataContent();

					formData.Add(
						new StreamContent(file.OpenReadStream()),
						"file",
						file.FileName
					);

					// Chỉ ảnh đầu tiên được set ảnh chính
					formData.Add(
						new StringContent((i == 0 && laAnhChinh).ToString()),
						"laAnhChinh"
					);

					var response = await _http.PostAsync(
						$"sanphams/chitiet/{sanPhamChiTietId}/upload-image",
						formData
					);

					if (!response.IsSuccessStatusCode)
					{
						var error = await response.Content.ReadAsStringAsync();

						return Json(new
						{
							success = false,
							message = error
						});
					}

					var result = await response.Content.ReadFromJsonAsync<UploadImageResponse>();

					if (result != null && !string.IsNullOrEmpty(result.UrlAnh))
					{
						uploadedUrls.Add(result.UrlAnh);
					}
				}

				return Json(new
				{
					success = true,
					urls = uploadedUrls
				});
			}
			catch (Exception ex)
			{
				return Json(new
				{
					success = false,
					message = ex.Message
				});
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
public class UploadImageResponse
{
	public string Message { get; set; }
	public string UrlAnh { get; set; }
	public Guid IDAnhSanPham { get; set; }
	public string MaAnh { get; set; }
	public bool LaAnhChinh { get; set; }
}
