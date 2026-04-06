using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using System.Text.Json;

namespace QuanView.Areas.Admin.Controllers
{
    [Area("Admin")]
    //[Authorize(Policy = "AdminPolicy")]
    public class QuanLyDonHangController : Controller
    {
        private readonly HttpClient _httpClient;

        private static readonly JsonSerializerOptions ApiJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public QuanLyDonHangController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("MyApi");
        }

        // DTO classes for API response
        public class HoaDonDto
        {
            public Guid IDHoaDon { get; set; }
            public string MaHoaDon { get; set; }
            public decimal TongTien { get; set; }
            public decimal? TienGiam { get; set; }
            public string TrangThai { get; set; }
            public DateTime NgayTao { get; set; }
            public string TenNguoiNhan { get; set; }
            public string SoDienThoaiNguoiNhan { get; set; }
            public string DiaChiGiaoHang { get; set; }
            public KhachHangDto KhachHang { get; set; }
            public NhanVienDto NhanVien { get; set; }
            public int SoLuongSanPham { get; set; }
        }

        public class KhachHangDto
        {
            public Guid IDKhachHang { get; set; }
            public string TenKhachHang { get; set; }
            public string SoDienThoai { get; set; }
        }

        public class NhanVienDto
        {
            public Guid IDNhanVien { get; set; }
            public string TenNhanVien { get; set; }
        }

        // DTO classes for pagination response
        public class PaginationInfo
        {
            public int TotalCount { get; set; }
            public int TotalPages { get; set; }
            public int CurrentPage { get; set; }
            public int PageSize { get; set; }
            public bool HasPreviousPage { get; set; }
            public bool HasNextPage { get; set; }
        }

        public class PaginatedResponse<T>
        {
            public List<T> Data { get; set; }
            public PaginationInfo Pagination { get; set; }
            public OrderStatistics Statistics { get; set; }
        }

        public class OrderStatistics
        {
            public int TotalOnlineCount { get; set; }
            public int TotalTaiQuayCount { get; set; }
            public int TotalPendingCount { get; set; }
        }

        // DTO classes for detailed invoice
        public class HoaDonDetailDto
        {
            public Guid IDHoaDon { get; set; }
            public string MaHoaDon { get; set; }
            public bool BanTaiQuay { get; set; }
            public decimal TongTien { get; set; }
            public decimal? TienGiam { get; set; }
            public decimal SoTienGiamTuDiem { get; set; }
            public int DiemDaDung { get; set; }
            public int DiemCong { get; set; }
            public decimal TyLeQuyDoiDiem { get; set; }
            public decimal? PhiVanChuyen { get; set; }
            public string TrangThai { get; set; }
            public DateTime NgayTao { get; set; }
            public string TenNguoiNhan { get; set; }
            public string SoDienThoaiNguoiNhan { get; set; }
            public string DiaChiGiaoHang { get; set; }
            public string GhiChu { get; set; }
            public string? LyDoHuyDon { get; set; }
            public KhachHangDetailDto KhachHang { get; set; }
            public NhanVienDetailDto NhanVien { get; set; }
            public PhieuGiamGiaDto PhieuGiamGia { get; set; }
            public PhuongThucThanhToanDto PhuongThucThanhToan { get; set; }
            public List<ChiTietHoaDonDetailDto> ChiTietHoaDons { get; set; }
        }

        public class KhachHangDetailDto
        {
            public Guid IDKhachHang { get; set; }
            public string TenKhachHang { get; set; }
            public string SoDienThoai { get; set; }
            public string Email { get; set; }
        }

        public class NhanVienDetailDto
        {
            public Guid IDNhanVien { get; set; }
            public string TenNhanVien { get; set; }
            public string SoDienThoai { get; set; }
        }

        public class PhieuGiamGiaDto
        {
            public Guid IDPhieuGiamGia { get; set; }
            public string MaPhieu { get; set; }
            public string TenPhieu { get; set; }
        }

        public class PhuongThucThanhToanDto
        {
            public Guid IDPhuongThucThanhToan { get; set; }
            public string TenPhuongThuc { get; set; }
        }

        public class ChiTietHoaDonDetailDto
        {
            public Guid IDChiTietHoaDon { get; set; }
            public string MaChiTietHoaDon { get; set; }
            public int SoLuong { get; set; }
            public decimal DonGia { get; set; }
            public decimal ThanhTien { get; set; }
            public SanPhamChiTietDetailDto SanPhamChiTiet { get; set; }
        }

        public class SanPhamChiTietDetailDto
        {
            public Guid IDSanPhamChiTiet { get; set; }
            public string MaSPChiTiet { get; set; }
            public decimal GiaBan { get; set; }
            public int SoLuongTonHienTai { get; set; }
            public int SoLuongDatMua { get; set; }
            public int SoLuongTonTruocXacNhan { get; set; }
            public int SoLuongTonDuKienSauHuy { get; set; }
            public KichCoDto KichCo { get; set; }
            public MauSacDto MauSac { get; set; }
            public HoaTietDto HoaTiet { get; set; }
            public SanPhamDetailDto SanPham { get; set; }
        }

        public class StockSnapshotDto
        {
            public int SoLuongTonHienTai { get; set; }
            public int SoLuongTonTruocXacNhan { get; set; }
            public int SoLuongTonDuKienSauHuy { get; set; }
        }

        public class KichCoDto { public string TenKichCo { get; set; } }
        public class MauSacDto { public string TenMauSac { get; set; } }
        public class HoaTietDto { public string TenHoaTiet { get; set; } }

        public class SanPhamDetailDto
        {
            public Guid IDSanPham { get; set; }
            public string TenSanPham { get; set; }
            public string MaSanPham { get; set; }
        }

        // GET: Admin/QuanLyDonHang
        public async Task<IActionResult> Index(string trangThai, string tuNgay, string denNgay, string loaiDonHang, string khachHang, string maDonHang, int page = 1, int pageSize = 10)
        {
            try
            {
                // Xây dựng URL với các tham số phân trang và lọc
                var url = $"HoaDons?page={page}&pageSize={pageSize}";

                if (!string.IsNullOrEmpty(trangThai))
                    url += $"&trangThai={Uri.EscapeDataString(trangThai)}";
                if (!string.IsNullOrEmpty(tuNgay))
                    url += $"&tuNgay={Uri.EscapeDataString(tuNgay)}";
                if (!string.IsNullOrEmpty(denNgay))
                    url += $"&denNgay={Uri.EscapeDataString(denNgay)}";
                if (!string.IsNullOrEmpty(loaiDonHang))
                    url += $"&loaiDonHang={Uri.EscapeDataString(loaiDonHang)}";
                if (!string.IsNullOrEmpty(khachHang))
                    url += $"&khachHang={Uri.EscapeDataString(khachHang)}";
                if (!string.IsNullOrEmpty(maDonHang))
                    url += $"&maDonHang={Uri.EscapeDataString(maDonHang)}";

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    // Đọc response content một lần duy nhất
                    var responseContent = await response.Content.ReadAsStringAsync();

                    try
                    {
                        // API trả về camelCase (data, pagination, totalCount...); deserialize không phân biệt hoa thường
                        var result = JsonSerializer.Deserialize<PaginatedResponse<HoaDonDto>>(
                            responseContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        );

                        if (result == null)
                        {
                            TempData["ErrorMessage"] = "API trả về dữ liệu không đúng định dạng.";
                            return View(new List<HoaDon>());
                        }

                        var hoaDonsData = result.Data ?? new List<HoaDonDto>();
                        var pagination = result.Pagination ?? new PaginationInfo();

                        var hoaDons = new List<HoaDon>();

                        // Convert DTO data to HoaDon objects for View compatibility
                        foreach (var hoaDonData in hoaDonsData)
                        {
                            var hoaDon = new HoaDon
                            {
                                IDHoaDon = hoaDonData.IDHoaDon,
                                MaHoaDon = hoaDonData.MaHoaDon ?? "",
                                TongTien = hoaDonData.TongTien,
                                TienGiam = hoaDonData.TienGiam ?? 0,
                                TrangThai = hoaDonData.TrangThai ?? "Chờ xác nhận",
                                NgayTao = hoaDonData.NgayTao,
                                TenNguoiNhan = hoaDonData.TenNguoiNhan,
                                SoDienThoaiNguoiNhan = hoaDonData.SoDienThoaiNguoiNhan,
                                DiaChiGiaoHang = hoaDonData.DiaChiGiaoHang
                            };

                            // Add KhachHang if exists
                            if (hoaDonData.KhachHang != null)
                            {
                                hoaDon.KhachHang = new KhachHang
                                {
                                    IDKhachHang = hoaDonData.KhachHang.IDKhachHang,
                                    TenKhachHang = hoaDonData.KhachHang.TenKhachHang,
                                    SoDienThoai = hoaDonData.KhachHang.SoDienThoai
                                };
                            }

                            // Add NhanVien if exists
                            if (hoaDonData.NhanVien != null)
                            {
                                hoaDon.NhanVien = new NhanVien
                                {
                                    IDNhanVien = hoaDonData.NhanVien.IDNhanVien,
                                    TenNhanVien = hoaDonData.NhanVien.TenNhanVien
                                };
                            }

                            hoaDons.Add(hoaDon);
                        }

                        // Tạo ViewBag cho thông tin phân trang
                        ViewBag.CurrentPage = pagination.CurrentPage;
                        ViewBag.PageSize = pagination.PageSize;
                        ViewBag.TotalPages = pagination.TotalPages;
                        ViewBag.TotalCount = pagination.TotalCount;
                        ViewBag.HasPreviousPage = pagination.HasPreviousPage;
                        ViewBag.HasNextPage = pagination.HasNextPage;
                        ViewBag.TotalOnlineCount = result.Statistics?.TotalOnlineCount ?? 0;
                        ViewBag.TotalTaiQuayCount = result.Statistics?.TotalTaiQuayCount ?? 0;
                        ViewBag.TotalPendingCount = result.Statistics?.TotalPendingCount ?? 0;

                        return View(hoaDons);
                    }
                    catch (System.Text.Json.JsonException jsonEx)
                    {
                        TempData["ErrorMessage"] = $"Lỗi khi xử lý dữ liệu từ API: {jsonEx.Message}";
                        return View(new List<HoaDon>());
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Lỗi API: {response.StatusCode} - {errorContent}";
                    return View(new List<HoaDon>());
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi tải danh sách đơn hàng: {ex.Message}";
                return View(new List<HoaDon>());
            }
        }

        // GET: Admin/QuanLyDonHang/Details/{id}
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"HoaDons/{id}");
                if (response.IsSuccessStatusCode)
                {
                    var hoaDonData = await response.Content.ReadFromJsonAsync<HoaDonDetailDto>(ApiJsonOptions);
                    if (hoaDonData != null)
                    {
                        var hoaDon = new HoaDon
                        {
                            IDHoaDon = hoaDonData.IDHoaDon,
                            MaHoaDon = hoaDonData.MaHoaDon,
                            BanTaiQuay = hoaDonData.BanTaiQuay,
                            TongTien = hoaDonData.TongTien,
                            TienGiam = hoaDonData.TienGiam ?? 0,
                            SoTienGiamTuDiem = hoaDonData.SoTienGiamTuDiem,
                            DiemDaDung = hoaDonData.DiemDaDung,
                            DiemCong = hoaDonData.DiemCong,
                            TyLeQuyDoiDiem = hoaDonData.TyLeQuyDoiDiem,
                            PhiVanChuyen = hoaDonData.PhiVanChuyen ?? 0,
                            TrangThai = hoaDonData.TrangThai,
                            NgayTao = hoaDonData.NgayTao,
                            TenNguoiNhan = hoaDonData.TenNguoiNhan,
                            SoDienThoaiNguoiNhan = hoaDonData.SoDienThoaiNguoiNhan,
                            DiaChiGiaoHang = hoaDonData.DiaChiGiaoHang,
                            LyDoHuyDon = hoaDonData.LyDoHuyDon,
                        };

                        // Add related data if exists
                        if (hoaDonData.KhachHang != null)
                        {
                            hoaDon.KhachHang = new KhachHang
                            {
                                IDKhachHang = hoaDonData.KhachHang.IDKhachHang,
                                TenKhachHang = hoaDonData.KhachHang.TenKhachHang,
                                SoDienThoai = hoaDonData.KhachHang.SoDienThoai,
                                Email = hoaDonData.KhachHang.Email
                            };
                        }

                        if (hoaDonData.NhanVien != null)
                        {
                            hoaDon.NhanVien = new NhanVien
                            {
                                IDNhanVien = hoaDonData.NhanVien.IDNhanVien,
                                TenNhanVien = hoaDonData.NhanVien.TenNhanVien,
                                SoDienThoai = hoaDonData.NhanVien.SoDienThoai
                            };
                        }

                        if (hoaDonData.PhuongThucThanhToan != null)
                        {
                            hoaDon.PhuongThucThanhToan = new PhuongThucThanhToan
                            {
                                IDPhuongThucThanhToan = hoaDonData.PhuongThucThanhToan.IDPhuongThucThanhToan,
                                TenPhuongThuc = hoaDonData.PhuongThucThanhToan.TenPhuongThuc
                            };
                        }

                        // Add ChiTietHoaDons if exists
                        if (hoaDonData.ChiTietHoaDons != null)
                        {
                            var stockSnapshots = new Dictionary<Guid, StockSnapshotDto>();
                            hoaDon.ChiTietHoaDons = new List<ChiTietHoaDon>();
                            foreach (var ct in hoaDonData.ChiTietHoaDons)
                            {
                                var chiTiet = new ChiTietHoaDon
                                {
                                    IDChiTietHoaDon = ct.IDChiTietHoaDon,
                                    MaChiTietHoaDon = ct.MaChiTietHoaDon,
                                    SoLuong = ct.SoLuong,
                                    DonGia = ct.DonGia,
                                    ThanhTien = ct.ThanhTien
                                };

                                if (ct.SanPhamChiTiet != null)
                                {
                                    chiTiet.SanPhamChiTiet = new SanPhamChiTiet
                                    {
                                        IDSanPhamChiTiet = ct.SanPhamChiTiet.IDSanPhamChiTiet,
                                        MaSPChiTiet = ct.SanPhamChiTiet.MaSPChiTiet,
                                        GiaBan = ct.SanPhamChiTiet.GiaBan,
                                        SoLuong = ct.SanPhamChiTiet.SoLuongTonHienTai,
                                        KichCo = ct.SanPhamChiTiet.KichCo != null ? new KichCo
                                        {
                                            TenKichCo = ct.SanPhamChiTiet.KichCo.TenKichCo
                                        } : null,
                                        MauSac = ct.SanPhamChiTiet.MauSac != null ? new MauSac
                                        {
                                            TenMauSac = ct.SanPhamChiTiet.MauSac.TenMauSac
                                        } : null,
                                        HoaTiet = ct.SanPhamChiTiet.HoaTiet != null ? new HoaTiet
                                        {
                                            TenHoaTiet = ct.SanPhamChiTiet.HoaTiet.TenHoaTiet
                                        } : null
                                    };

                                    stockSnapshots[chiTiet.IDChiTietHoaDon] = new StockSnapshotDto
                                    {
                                        SoLuongTonHienTai = ct.SanPhamChiTiet.SoLuongTonHienTai,
                                        SoLuongTonTruocXacNhan = ct.SanPhamChiTiet.SoLuongTonTruocXacNhan,
                                        SoLuongTonDuKienSauHuy = ct.SanPhamChiTiet.SoLuongTonDuKienSauHuy
                                    };

                                    if (ct.SanPhamChiTiet.SanPham != null)
                                    {
                                        chiTiet.SanPhamChiTiet.SanPham = new SanPham
                                        {
                                            IDSanPham = ct.SanPhamChiTiet.SanPham.IDSanPham,
                                            TenSanPham = ct.SanPhamChiTiet.SanPham.TenSanPham,
                                            MaSanPham = ct.SanPhamChiTiet.SanPham.MaSanPham
                                        };
                                    }

                                }

                                hoaDon.ChiTietHoaDons.Add(chiTiet);
                            }

                            ViewBag.StockSnapshots = stockSnapshots;
                        }

                        return View(hoaDon);
                    }
                }
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi tải chi tiết đơn hàng: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/QuanLyDonHang/CapNhatTrangThai/{id}
        [HttpPost]
        public async Task<IActionResult> CapNhatTrangThai(Guid id, string trangThaiMoi)
        {
            try
            {
                var hoaDon = new
                {
                    trangThai = trangThaiMoi,
                    nguoiCapNhat = User.Identity?.Name ?? "Admin",
                    lanCapNhatCuoi = DateTime.UtcNow
                };

                var response = await _httpClient.PutAsJsonAsync($"HoaDons/{id}/trangthai", hoaDon);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = $"Đã cập nhật trạng thái đơn hàng thành '{trangThaiMoi}'";
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Lỗi khi cập nhật trạng thái: {error}";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Admin/QuanLyDonHang/XacNhanDonHang/{id}
        [HttpPost]
        public async Task<IActionResult> XacNhanDonHang(Guid id)
        {
            return await CapNhatTrangThai(id, "Đã xác nhận");
        }

        // POST: Admin/QuanLyDonHang/XacNhanLayHang/{id}
        [HttpPost]
        public async Task<IActionResult> XacNhanLayHang(Guid id)
        {
            // Lấy trạng thái hiện tại để xác định trạng thái tiếp theo
            var response = await _httpClient.GetAsync($"HoaDons/{id}");
            if (response.IsSuccessStatusCode)
            {
                var hoaDon = await response.Content.ReadFromJsonAsync<HoaDon>(ApiJsonOptions);
                if (hoaDon != null)
                {
                    string trangThaiMoi = hoaDon.TrangThai switch
                    {
                        "Đã xác nhận" => "Chờ lấy hàng",
                        "Chờ lấy hàng" => "Chờ lấy hàng",
                        _ => "Chờ lấy hàng"
                    };
                    return await CapNhatTrangThai(id, trangThaiMoi);
                }
            }
            return await CapNhatTrangThai(id, "Chờ lấy hàng");
        }

        // POST: Admin/QuanLyDonHang/XacNhanGiaoHang/{id}
        [HttpPost]
        public async Task<IActionResult> XacNhanGiaoHang(Guid id)
        {
            // Lấy trạng thái hiện tại để xác định trạng thái tiếp theo
            var response = await _httpClient.GetAsync($"HoaDons/{id}");
            if (response.IsSuccessStatusCode)
            {
                var hoaDon = await response.Content.ReadFromJsonAsync<HoaDon>(ApiJsonOptions);
                if (hoaDon != null)
                {
                    string trangThaiMoi = hoaDon.TrangThai switch
                    {
                        "Chờ lấy hàng" => "Đang giao",
                        "Đang giao" => "Đang giao",
                        "Đã lấy hàng" => "Đang giao",
                        "Chờ giao hàng" => "Đang giao",
                        "Đang giao hàng" => "Đang giao",
                        _ => "Đang giao"
                    };
                    return await CapNhatTrangThai(id, trangThaiMoi);
                }
            }
            return await CapNhatTrangThai(id, "Đang giao");
        }

        // POST: Admin/QuanLyDonHang/XacNhanDaGiaoHang/{id}
        [HttpPost]
        public async Task<IActionResult> XacNhanDaGiaoHang(Guid id)
        {
            // Lấy trạng thái hiện tại để xác định trạng thái tiếp theo
            var response = await _httpClient.GetAsync($"HoaDons/{id}");
            if (response.IsSuccessStatusCode)
            {
                var hoaDon = await response.Content.ReadFromJsonAsync<HoaDon>(ApiJsonOptions);
                if (hoaDon != null)
                {
                    string trangThaiMoi = hoaDon.TrangThai switch
                    {
                        "Đang giao" => "Đã giao",
                        "Đang giao hàng" => "Đã giao",
                        "Đã giao" => "Đã giao",
                        _ => "Đã giao"
                    };
                    return await CapNhatTrangThai(id, trangThaiMoi);
                }
            }
            return await CapNhatTrangThai(id, "Đã giao");
        }

        // POST: Admin/QuanLyDonHang/HuyDonHang/{id}
        [HttpPost]
        public async Task<IActionResult> HuyDonHang(Guid id)
        {
            try
            {
                var detailResponse = await _httpClient.GetAsync($"HoaDons/{id}");
                if (!detailResponse.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var hoaDon = await detailResponse.Content.ReadFromJsonAsync<HoaDonDetailDto>(ApiJsonOptions);
                if (hoaDon == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng";
                    return RedirectToAction(nameof(Details), new { id });
                }

                if (hoaDon.BanTaiQuay)
                {
                    TempData["ErrorMessage"] = "Chỉ cho phép hủy đơn online ở màn hình này.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                if (hoaDon.TrangThai == "Đã hủy")
                {
                    TempData["ErrorMessage"] = "Đơn hàng đã ở trạng thái hủy.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var payload = new
                {
                    TrangThai = "Đã hủy",
                    NguoiCapNhat = User.Identity?.Name ?? "Admin",
                    LanCapNhatCuoi = DateTime.UtcNow,
                    LyDoHuyDon = "Hủy đơn bởi quản trị viên"
                };

                var updateResponse = await _httpClient.PutAsJsonAsync($"HoaDons/{id}/trangthai", payload);

                if (updateResponse.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Đã hủy đơn hàng thành công.";
                }
                else
                {
                    var error = await updateResponse.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Lỗi khi hủy đơn hàng: {error}";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi hủy đơn hàng: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Admin/QuanLyDonHang/HoanTien/{id}
        [HttpPost]
        public async Task<IActionResult> HoanTien(Guid id)
        {
            try
            {
                var detailResponse = await _httpClient.GetAsync($"HoaDons/{id}");
                if (!detailResponse.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var hoaDon = await detailResponse.Content.ReadFromJsonAsync<HoaDonDetailDto>(ApiJsonOptions);
                if (hoaDon == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng";
                    return RedirectToAction(nameof(Details), new { id });
                }

                if (hoaDon.BanTaiQuay)
                {
                    TempData["ErrorMessage"] = "Chỉ hỗ trợ hoàn tiền cho đơn online.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                if (hoaDon.TrangThai == "Đã hoàn tiền")
                {
                    TempData["ErrorMessage"] = "Đơn hàng đã được hoàn tiền trước đó.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                if (hoaDon.TrangThai != "Đã hủy")
                {
                    TempData["ErrorMessage"] = "Chỉ hoàn tiền cho đơn hàng đã hủy.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var tenPhuongThucThanhToan = hoaDon.PhuongThucThanhToan?.TenPhuongThuc ?? string.Empty;
                if (!tenPhuongThucThanhToan.Contains("chuyển khoản", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["ErrorMessage"] = "Chỉ hỗ trợ hoàn tiền cho đơn thanh toán chuyển khoản.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var payload = new
                {
                    TrangThai = "Đã hoàn tiền",
                    NguoiCapNhat = User.Identity?.Name ?? "Admin",
                    LanCapNhatCuoi = DateTime.UtcNow
                };

                var updateResponse = await _httpClient.PutAsJsonAsync($"HoaDons/{id}/trangthai", payload);
                if (updateResponse.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Đã hoàn tiền cho đơn hàng thành công.";
                }
                else
                {
                    var error = await updateResponse.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Lỗi khi hoàn tiền: {error}";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi hoàn tiền: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Admin/QuanLyDonHang/Rollback/{id}
        [HttpPost]
        public async Task<IActionResult> Rollback(Guid id)
        {
            try
            {
                // Lấy thông tin đơn hàng hiện tại để xác định trạng thái rollback
                var hoaDonResponse = await _httpClient.GetAsync($"HoaDons/{id}");
                if (!hoaDonResponse.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var hoaDon = await hoaDonResponse.Content.ReadFromJsonAsync<HoaDonDetailDto>(ApiJsonOptions);
                if (hoaDon == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Xác định trạng thái rollback dựa trên trạng thái hiện tại
                string targetStatus = hoaDon.TrangThai switch
                {
                    "Đã xác nhận" => "Chờ xác nhận",
                    "Chờ lấy hàng" => "Đã xác nhận",
                    "Đã lấy hàng" => "Chờ lấy hàng",
                    "Chờ giao hàng" => "Chờ lấy hàng",
                    "Đang giao" => "Chờ lấy hàng",
                    "Đang giao hàng" => "Chờ lấy hàng",
                    "Đã giao" => "Đang giao",
                    "Giao hàng thành công" => "Đã giao",
                    _ => ""
                };

                if (string.IsNullOrEmpty(targetStatus))
                {
                    TempData["ErrorMessage"] = "Không thể rollback trạng thái này";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Tạo DTO cho rollback
                var rollbackDto = new
                {
                    TargetStatus = targetStatus,
                    Reason = $"Rollback từ '{hoaDon.TrangThai}' về '{targetStatus}' bởi admin",
                    UpdatedBy = User.Identity?.Name ?? "Admin"
                };

                var jsonContent = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(rollbackDto),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync($"HoaDons/{id}/rollback", jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = $"Đã rollback trạng thái từ '{hoaDon.TrangThai}' về '{targetStatus}' thành công";
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Lỗi khi rollback trạng thái: {error}";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Admin/QuanLyDonHang/DonHangCanXacNhan
        [HttpGet]
        public async Task<IActionResult> DonHangCanXacNhan()
        {
            try
            {
                var response = await _httpClient.GetAsync("HoaDons/don-hang-can-xac-nhan");
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<object>();
                    return Ok(result);
                }
                return StatusCode((int)response.StatusCode, "Lỗi khi lấy đơn hàng cần xác nhận");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi: {ex.Message}");
            }
        }
        // GET: Admin/QuanLyDonHang/XuatHoaDon/{id}
        [HttpGet]
        public async Task<IActionResult> XuatHoaDon(Guid id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"HoaDons/{id}");
                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var hoaDonData = await response.Content.ReadFromJsonAsync<HoaDonDetailDto>(ApiJsonOptions);
                if (hoaDonData == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var hoaDon = new HoaDon
                {
                    IDHoaDon = hoaDonData.IDHoaDon,
                    MaHoaDon = hoaDonData.MaHoaDon ?? "",
                    TongTien = hoaDonData.TongTien,
                    TienGiam = hoaDonData.TienGiam ?? 0,
                    SoTienGiamTuDiem = hoaDonData.SoTienGiamTuDiem,
                    DiemDaDung = hoaDonData.DiemDaDung,
                    DiemCong = hoaDonData.DiemCong,
                    TyLeQuyDoiDiem = hoaDonData.TyLeQuyDoiDiem,
                    PhiVanChuyen = hoaDonData.PhiVanChuyen ?? 0,
                    TrangThai = hoaDonData.TrangThai ?? "Chờ xác nhận",
                    NgayTao = hoaDonData.NgayTao,
                    TenNguoiNhan = hoaDonData.TenNguoiNhan,
                    SoDienThoaiNguoiNhan = hoaDonData.SoDienThoaiNguoiNhan,
                    DiaChiGiaoHang = hoaDonData.DiaChiGiaoHang,
                };

                if (hoaDonData.KhachHang != null)
                {
                    hoaDon.KhachHang = new KhachHang
                    {
                        IDKhachHang = hoaDonData.KhachHang.IDKhachHang,
                        TenKhachHang = hoaDonData.KhachHang.TenKhachHang,
                        SoDienThoai = hoaDonData.KhachHang.SoDienThoai,
                        Email = hoaDonData.KhachHang.Email
                    };
                }

                if (hoaDonData.PhuongThucThanhToan != null)
                {
                    hoaDon.PhuongThucThanhToan = new PhuongThucThanhToan
                    {
                        IDPhuongThucThanhToan = hoaDonData.PhuongThucThanhToan.IDPhuongThucThanhToan,
                        TenPhuongThuc = hoaDonData.PhuongThucThanhToan.TenPhuongThuc
                    };
                }

                if (hoaDonData.ChiTietHoaDons != null)
                {
                    hoaDon.ChiTietHoaDons = new List<ChiTietHoaDon>();
                    foreach (var ct in hoaDonData.ChiTietHoaDons)
                    {
                        var chiTiet = new ChiTietHoaDon
                        {
                            IDChiTietHoaDon = ct.IDChiTietHoaDon,
                            MaChiTietHoaDon = ct.MaChiTietHoaDon,
                            SoLuong = ct.SoLuong,
                            DonGia = ct.DonGia,
                            ThanhTien = ct.ThanhTien
                        };

                        if (ct.SanPhamChiTiet != null)
                        {
                            chiTiet.SanPhamChiTiet = new SanPhamChiTiet
                            {
                                IDSanPhamChiTiet = ct.SanPhamChiTiet.IDSanPhamChiTiet,
                                MaSPChiTiet = ct.SanPhamChiTiet.MaSPChiTiet,
                                GiaBan = ct.SanPhamChiTiet.GiaBan,
                                KichCo = ct.SanPhamChiTiet.KichCo != null ? new KichCo
                                {
                                    TenKichCo = ct.SanPhamChiTiet.KichCo.TenKichCo
                                } : null,
                                MauSac = ct.SanPhamChiTiet.MauSac != null ? new MauSac
                                {
                                    TenMauSac = ct.SanPhamChiTiet.MauSac.TenMauSac
                                } : null,
                                HoaTiet = ct.SanPhamChiTiet.HoaTiet != null ? new HoaTiet
                                {
                                    TenHoaTiet = ct.SanPhamChiTiet.HoaTiet.TenHoaTiet
                                } : null,
                                SanPham = ct.SanPhamChiTiet.SanPham != null ? new SanPham
                                {
                                    IDSanPham = ct.SanPhamChiTiet.SanPham.IDSanPham,
                                    TenSanPham = ct.SanPhamChiTiet.SanPham.TenSanPham,
                                    MaSanPham = ct.SanPhamChiTiet.SanPham.MaSanPham
                                } : null
                            };
                        }

                        hoaDon.ChiTietHoaDons.Add(chiTiet);
                    }
                }

                return View("Invoice", hoaDon);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi xuất hóa đơn: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id });
            }
        }
    }
    // Lỗi CS0234: The type or namespace name 'Dtos' does not exist in the namespace 'QuanApi' (are you missing an assembly reference?)
    // Nguyên nhân: Không tìm thấy namespace QuanApi.Dtos. Có thể bạn chưa thêm reference đến project hoặc assembly chứa namespace này, hoặc namespace bị sai chính tả.
    // Cách khắc phục: 
    // 1. Đảm bảo project QuanApi có thư mục/namespace Dtos và các class DTO cần thiết.
    // 2. Nếu QuanApi là project khác, hãy Add Reference từ QuanView sang QuanApi.
    // 3. Nếu namespace bị sai, hãy sửa lại đúng tên namespace trong file using.
    // 4. Nếu không cần thiết, hãy xóa dòng using QuanApi.Dtos; nếu các DTO đã được định nghĩa trong file này.
}