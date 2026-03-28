using Microsoft.AspNetCore.Mvc;
using QuanView.Areas.Admin.Models;

namespace QuanView.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CauHinhBanHangController : Controller
    {
        private readonly HttpClient _http;

        public CauHinhBanHangController(IHttpClientFactory factory)
        {
            _http = factory.CreateClient("MyApi");
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var httpResponse = await _http.GetAsync("CauHinhBanHang");
                if (!httpResponse.IsSuccessStatusCode)
                {
                    var errorBody = await httpResponse.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = string.IsNullOrWhiteSpace(errorBody)
                        ? $"Không thể tải cấu hình ship và điểm. API trả về mã {(int)httpResponse.StatusCode}."
                        : $"Không thể tải cấu hình ship và điểm. {errorBody}";
                    return View(new CauHinhBanHangPageVm());
                }

                var response = await httpResponse.Content.ReadFromJsonAsync<CauHinhBanHangPageVm>();
                return View(response ?? new CauHinhBanHangPageVm());
            }
            catch (HttpRequestException)
            {
                TempData["ErrorMessage"] = "Không thể kết nối tới API cấu hình ship và điểm. Hãy kiểm tra API đang chạy và database đã được cập nhật migration.";
                return View(new CauHinhBanHangPageVm());
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Tải cấu hình thất bại: {ex.Message}";
                return View(new CauHinhBanHangPageVm());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveConfig(CauHinhBanHangConfigVm config)
        {
            var payload = new
            {
                config.PhiShipMacDinh,
                config.PhiShipNoiThanh,
                config.PhiShipNgoaiThanh,
                config.PhiShipToanQuoc,
                config.TinhApDungPhiShip,
                config.DanhSachQuanHuyenNoiThanh,
                config.NguonTinhPhiShipMacDinh,
                config.SoTienTrenMotDiemTich,
                config.SoTienGiamTrenMotDiem,
                config.DiemToiDaSuDungMoiDon,
                NguoiCapNhat = User.Identity?.Name ?? "Admin"
            };

            var response = await _http.PutAsJsonAsync("CauHinhBanHang", payload);
            TempData[response.IsSuccessStatusCode ? "SuccessMessage" : "ErrorMessage"] =
                response.IsSuccessStatusCode ? "Cập nhật cấu hình thành công." : "Cập nhật cấu hình thất bại.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveTier(HangKhachHangVm tier)
        {
            HttpResponseMessage response;
            if (tier.IDHangKhachHang == Guid.Empty)
            {
                response = await _http.PostAsJsonAsync("CauHinhBanHang/tiers", tier);
            }
            else
            {
                response = await _http.PutAsJsonAsync($"CauHinhBanHang/tiers/{tier.IDHangKhachHang}", tier);
            }

            TempData[response.IsSuccessStatusCode ? "SuccessMessage" : "ErrorMessage"] =
                response.IsSuccessStatusCode ? "Lưu hạng khách hàng thành công." : "Lưu hạng khách hàng thất bại.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTier(Guid id)
        {
            var response = await _http.DeleteAsync($"CauHinhBanHang/tiers/{id}");
            TempData[response.IsSuccessStatusCode ? "SuccessMessage" : "ErrorMessage"] =
                response.IsSuccessStatusCode ? "Đã ngừng sử dụng hạng khách hàng." : "Không thể ngừng sử dụng hạng khách hàng.";
            return RedirectToAction(nameof(Index));
        }
    }
}
