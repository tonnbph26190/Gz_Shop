using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Json;
using QuanApi.Data;
using QuanApi.Dtos;

namespace QuanView.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminPolicy")]
    public class QuanLyDonHangController : Controller
    {
        private readonly HttpClient _httpClient;

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
        }

        // DTO classes for detailed invoice
        public class HoaDonDetailDto
        {
            public Guid IDHoaDon { get; set; }
            public string MaHoaDon { get; set; }
            public decimal TongTien { get; set; }
            public decimal? TienGiam { get; set; }
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
            public KichCoDto KichCo { get; set; }
            public MauSacDto MauSac { get; set; }
            public HoaTietDto HoaTiet { get; set; }
            public SanPhamDetailDto SanPham { get; set; }
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
                    Console.WriteLine($"API Response: {responseContent}");
                    
                    try
                    {
                        // Sử dụng JsonSerializer để deserialize từ string đã đọc
                        var result = System.Text.Json.JsonSerializer.Deserialize<PaginatedResponse<HoaDonDto>>(
                            responseContent, 
                            new System.Text.Json.JsonSerializerOptions 
                            { 
                                PropertyNameCaseInsensitive = true 
                            }
                        );
                        
                        var hoaDonsData = result?.Data ?? new List<HoaDonDto>();
                        var pagination = result?.Pagination ?? new PaginationInfo();
                        
                        var hoaDons = new List<HoaDon>();

                        // Convert DTO data to HoaDon objects for View compatibility
                        foreach (var hoaDonData in hoaDonsData)
                        {
                            var hoaDon = new HoaDon
                            {
                                IDHoaDon = hoaDonData.IDHoaDon,
                                MaHoaDon = hoaDonData.MaHoaDon,
                                TongTien = hoaDonData.TongTien,
                                TienGiam = hoaDonData.TienGiam ?? 0,
                                TrangThai = hoaDonData.TrangThai,
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

                        return View(hoaDons);
                    }
                    catch (System.Text.Json.JsonException jsonEx)
                    {
                        Console.WriteLine($"JSON Deserialization Error: {jsonEx.Message}");
                        TempData["ErrorMessage"] = $"Lỗi khi xử lý dữ liệu từ API: {jsonEx.Message}";
                        return View(new List<HoaDon>());
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API Error: {response.StatusCode} - {errorContent}");
                    TempData["ErrorMessage"] = $"Lỗi API: {response.StatusCode} - {errorContent}";
                    return View(new List<HoaDon>());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error: {ex.Message}");
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
                    var hoaDonData = await response.Content.ReadFromJsonAsync<HoaDonDetailDto>();
                    if (hoaDonData != null)
                    {
                        var hoaDon = new HoaDon
                        {
                            IDHoaDon = hoaDonData.IDHoaDon,
                            MaHoaDon = hoaDonData.MaHoaDon,
                            TongTien = hoaDonData.TongTien,
                            TienGiam = hoaDonData.TienGiam ?? 0,
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
                                        } : null
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
                var hoaDon = await response.Content.ReadFromJsonAsync<HoaDon>();
                if (hoaDon != null)
                {
                    string trangThaiMoi = hoaDon.TrangThai switch
                    {
                        "Đã xác nhận" => "Chờ lấy hàng",
                        "Chờ lấy hàng" => "Đã lấy hàng",
                        _ => "Đã lấy hàng"
                    };
                    return await CapNhatTrangThai(id, trangThaiMoi);
                }
            }
            return await CapNhatTrangThai(id, "Đã lấy hàng");
        }

        // POST: Admin/QuanLyDonHang/XacNhanGiaoHang/{id}
        [HttpPost]
        public async Task<IActionResult> XacNhanGiaoHang(Guid id)
        {
            // Lấy trạng thái hiện tại để xác định trạng thái tiếp theo
            var response = await _httpClient.GetAsync($"HoaDons/{id}");
            if (response.IsSuccessStatusCode)
            {
                var hoaDon = await response.Content.ReadFromJsonAsync<HoaDon>();
                if (hoaDon != null)
                {
                    string trangThaiMoi = hoaDon.TrangThai switch
                    {
                        "Đã lấy hàng" => "Chờ giao hàng",
                        "Chờ giao hàng" => "Đang giao hàng",
                        _ => "Đang giao hàng"
                    };
                    return await CapNhatTrangThai(id, trangThaiMoi);
                }
            }
            return await CapNhatTrangThai(id, "Đang giao hàng");
        }

        // POST: Admin/QuanLyDonHang/XacNhanDaGiaoHang/{id}
        [HttpPost]
        public async Task<IActionResult> XacNhanDaGiaoHang(Guid id)
        {
            // Lấy trạng thái hiện tại để xác định trạng thái tiếp theo
            var response = await _httpClient.GetAsync($"HoaDons/{id}");
            if (response.IsSuccessStatusCode)
            {
                var hoaDon = await response.Content.ReadFromJsonAsync<HoaDon>();
                if (hoaDon != null)
                {
                    string trangThaiMoi = hoaDon.TrangThai switch
                    {
                        "Đang giao hàng" => "Đã giao",
                        "Đã giao" => "Giao hàng thành công",
                        _ => "Giao hàng thành công"
                    };
                    return await CapNhatTrangThai(id, trangThaiMoi);
                }
            }
            return await CapNhatTrangThai(id, "Giao hàng thành công");
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

                var hoaDon = await hoaDonResponse.Content.ReadFromJsonAsync<HoaDonDetailDto>();
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
                    "Chờ giao hàng" => "Đã lấy hàng",
                    "Đang giao hàng" => "Chờ giao hàng",
                    "Đã giao" => "Đang giao hàng",
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

                var hoaDonData = await response.Content.ReadFromJsonAsync<HoaDonDetailDto>();
                if (hoaDonData == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var hoaDon = new HoaDon
                {
                    IDHoaDon = hoaDonData.IDHoaDon,
                    MaHoaDon = hoaDonData.MaHoaDon,
                    TongTien = hoaDonData.TongTien,
                    TienGiam = hoaDonData.TienGiam ?? 0,
                    PhiVanChuyen = hoaDonData.PhiVanChuyen ?? 0,
                    TrangThai = hoaDonData.TrangThai,
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
}