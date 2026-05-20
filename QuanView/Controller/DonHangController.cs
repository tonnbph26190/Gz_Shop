using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using QuanApi.Dtos;
using System.Text;
using System.Text.Json;

namespace QuanView.Controllers
{
    public class DonHangController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DonHangController> _logger;

        /// <summary>Giữ cùng kích thước trang với API để tránh phản hồi quá lớn.</summary>
        private const int CustomerOrdersPageSize = 10;

        public DonHangController(IHttpClientFactory httpClientFactory, ILogger<DonHangController> logger)
        {
            _httpClient = httpClientFactory.CreateClient("MyApi");
            _logger = logger;
        }

        private Guid? ResolveCurrentCustomerId()
        {
            var claim = User.FindFirst("custom:id_khachhang")?.Value;
            if (!string.IsNullOrEmpty(claim) && Guid.TryParse(claim, out var fromClaim))
                return fromClaim;

            var sessionId = HttpContext.Session.GetString("CustomerId");
            if (!string.IsNullOrEmpty(sessionId) && Guid.TryParse(sessionId, out var fromSession))
                return fromSession;

            return null;
        }

        // GET: DonHang/Index
        public async Task<IActionResult> Index(string search = "", string fromDate = "", string toDate = "", string quickDate = "", int page = 1)
        {
            try
            {
                // Đồng bộ quickDate với from/to để pagination hoặc reload không làm mất bộ lọc.
                if (quickDate == "")
                {
                    fromDate = "";
                    toDate = "";
                }
                else if (!string.Equals(quickDate, "custom", StringComparison.OrdinalIgnoreCase)
                         && !string.IsNullOrEmpty(quickDate)
                         && string.IsNullOrEmpty(fromDate)
                         && string.IsNullOrEmpty(toDate))
                {
                    var today = DateTime.UtcNow.Date;
                    var startDate = quickDate == "today"
                        ? today
                        : (int.TryParse(quickDate, out var days) ? today.AddDays(-days) : today);

                    fromDate = startDate.ToString("yyyy-MM-dd");
                    toDate = today.ToString("yyyy-MM-dd");
                }

                ViewBag.QuickDate = quickDate;
                var isAuthenticated = User.Identity.IsAuthenticated;
                var customerId = ResolveCurrentCustomerId();

                // Nếu người dùng đã đăng nhập, lấy đơn hàng của họ
                if (isAuthenticated && customerId.HasValue)
                {
                    var customerParameters = new List<string>();

                    if (!string.IsNullOrEmpty(search))
                    {
                        customerParameters.Add($"search={Uri.EscapeDataString(search)}");
                    }

                    if (!string.IsNullOrEmpty(fromDate))
                    {
                        customerParameters.Add($"fromDate={Uri.EscapeDataString(fromDate)}");
                    }

                    if (!string.IsNullOrEmpty(toDate))
                    {
                        customerParameters.Add($"toDate={Uri.EscapeDataString(toDate)}");
                    }

                    customerParameters.Add($"page={page}");
                    customerParameters.Add($"pageSize={CustomerOrdersPageSize}");

                    var query = customerParameters.Count > 0 ? "?" + string.Join("&", customerParameters) : "";
                    var customerApiUrl = $"HoaDons/customer/{customerId.Value}{query}";

                    // Chỉ đọc headers trước để tránh buffering toàn bộ response ở GetAsync
                    var customerResponse = await _httpClient.GetAsync(customerApiUrl, HttpCompletionOption.ResponseHeadersRead);

                    if (customerResponse.IsSuccessStatusCode)
                    {
                        var hoaDons = await customerResponse.Content.ReadFromJsonAsync<List<HoaDon>>();
                        var filteredHoaDons = hoaDons ?? new List<HoaDon>();

                        var totalCount = 0;
                        var totalPages = 0;
                        var currentPage = page;
                        var pageSize = CustomerOrdersPageSize;

                        if (customerResponse.Headers.Contains("X-Total-Count"))
                            int.TryParse(customerResponse.Headers.GetValues("X-Total-Count").FirstOrDefault(), out totalCount);
                        if (customerResponse.Headers.Contains("X-Total-Pages"))
                            int.TryParse(customerResponse.Headers.GetValues("X-Total-Pages").FirstOrDefault(), out totalPages);
                        if (totalCount <= 0)
                            totalCount = filteredHoaDons.Count;
                        if (totalPages <= 0 && totalCount > 0)
                            totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                        if (totalPages <= 0)
                            totalPages = 1;

                        var viewModel = new
                        {
                            HoaDons = filteredHoaDons,
                            Pagination = new
                            {
                                CurrentPage = currentPage,
                                TotalPages = totalPages,
                                TotalCount = totalCount,
                                PageSize = pageSize,
                                HasPreviousPage = currentPage > 1,
                                HasNextPage = currentPage < totalPages
                            }
                        };

                        ViewBag.Search = search;
                        ViewBag.FromDate = fromDate;
                        ViewBag.ToDate = toDate;
                        ViewBag.IsAuthenticated = true;
                        return View(viewModel);
                    }

                    var err = await customerResponse.Content.ReadAsStringAsync();
                    _logger.LogWarning("Không lấy được đơn hàng khách {CustomerId}: {Status} — {Body}", customerId, customerResponse.StatusCode, err);
                    ViewBag.Search = search;
                    ViewBag.FromDate = fromDate;
                    ViewBag.ToDate = toDate;
                    ViewBag.IsAuthenticated = true;
                    TempData["ErrorMessage"] = "Không tải được danh sách đơn hàng. Vui lòng thử lại sau.";
                    return View(new
                    {
                        HoaDons = new List<HoaDon>(),
                        Pagination = new
                        {
                            CurrentPage = page,
                            TotalPages = 0,
                            TotalCount = 0,
                            PageSize = CustomerOrdersPageSize,
                            HasPreviousPage = false,
                            HasNextPage = false
                        }
                    });
                }

                if (isAuthenticated && !customerId.HasValue)
                {
                    _logger.LogWarning("Người dùng đã đăng nhập nhưng không có custom:id_khachhang hoặc CustomerId trong session.");
                    ViewBag.Search = search;
                    ViewBag.FromDate = fromDate;
                    ViewBag.ToDate = toDate;
                    ViewBag.IsAuthenticated = true;
                    TempData["ErrorMessage"] = "Không xác định được tài khoản khách hàng. Vui lòng đăng xuất và đăng nhập lại.";
                    return View(new
                    {
                        HoaDons = new List<HoaDon>(),
                        Pagination = new
                        {
                            CurrentPage = page,
                            TotalPages = 0,
                            TotalCount = 0,
                            PageSize = CustomerOrdersPageSize,
                            HasPreviousPage = false,
                            HasNextPage = false
                        }
                    });
                }

                //if (string.IsNullOrEmpty(search))
                //{
                //    ViewBag.Search = search;
                //    ViewBag.FromDate = fromDate;
                //    ViewBag.ToDate = toDate;
                //    ViewBag.IsAuthenticated = isAuthenticated;

                //    var emptyViewModel = new
                //    {
                //        HoaDons = new List<HoaDon>(),
                //        Pagination = new
                //        {
                //            CurrentPage = page,
                //            TotalPages = 0,
                //            TotalCount = 0,
                //            PageSize = 10,
                //            HasPreviousPage = false,
                //            HasNextPage = false
                //        }
                //    };
                //    return View(emptyViewModel);
                //}

                var parameters = new List<string>();

                if (!string.IsNullOrEmpty(search))
                {
                    parameters.Add($"search={Uri.EscapeDataString(search)}");
                }

                parameters.Add($"page={page}");
                parameters.Add("pageSize=10");

                var queryGuest = parameters.Count > 0 ? "?" + string.Join("&", parameters) : "";
                var apiUrl = $"HoaDons/guest{queryGuest}";

                var response = await _httpClient.GetAsync(apiUrl, HttpCompletionOption.ResponseHeadersRead);

                if (response.IsSuccessStatusCode)
                {
                    var hoaDons = await response.Content.ReadFromJsonAsync<List<HoaDon>>();
                    var filteredHoaDons = hoaDons ?? new List<HoaDon>();

                    var totalCount = 0;
                    var totalPages = 0;
                    var currentPage = page;
                    var pageSize = 10;

                    if (response.Headers.Contains("X-Total-Count"))
                        int.TryParse(response.Headers.GetValues("X-Total-Count").FirstOrDefault(), out totalCount);
                    if (response.Headers.Contains("X-Total-Pages"))
                        int.TryParse(response.Headers.GetValues("X-Total-Pages").FirstOrDefault(), out totalPages);
                    if (totalCount <= 0)
                        totalCount = filteredHoaDons.Count;
                    if (totalPages <= 0 && totalCount > 0)
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                    if (totalPages <= 0)
                        totalPages = 1;

                    var viewModel = new
                    {
                        HoaDons = filteredHoaDons,
                        Pagination = new
                        {
                            CurrentPage = currentPage,
                            TotalPages = totalPages,
                            TotalCount = totalCount,
                            PageSize = pageSize,
                            HasPreviousPage = currentPage > 1,
                            HasNextPage = currentPage < totalPages
                        }
                    };

                    ViewBag.Search = search;
                    ViewBag.FromDate = fromDate;
                    ViewBag.ToDate = toDate;
                    ViewBag.IsAuthenticated = isAuthenticated;
                    return View(viewModel);
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("API trả về lỗi: {StatusCode} - {ErrorMessage}", response.StatusCode, errorMessage);

                    if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        ModelState.AddModelError("Search", errorMessage);
                    }

                    ViewBag.Search = search;
                    ViewBag.FromDate = fromDate;
                    ViewBag.ToDate = toDate;
                    ViewBag.IsAuthenticated = isAuthenticated;

                    var emptyViewModel = new
                    {
                        HoaDons = new List<HoaDon>(),
                        Pagination = new
                        {
                            CurrentPage = page,
                            TotalPages = 0,
                            TotalCount = 0,
                            PageSize = 10,
                            HasPreviousPage = false,
                            HasNextPage = false
                        }
                    };
                    return View(emptyViewModel);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Lỗi kết nối API trong DonHang Index");

                ViewBag.Search = search;
                ViewBag.FromDate = fromDate;
                ViewBag.ToDate = toDate;
                ViewBag.QuickDate = quickDate;
                ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;
                TempData["ErrorMessage"] = "Không kết nối được tới hệ thống đơn hàng. Vui lòng thử lại sau.";

                var emptyViewModel = new
                {
                    HoaDons = new List<HoaDon>(),
                    Pagination = new
                    {
                        CurrentPage = page,
                        TotalPages = 0,
                        TotalCount = 0,
                        PageSize = 10,
                        HasPreviousPage = false,
                        HasNextPage = false
                    }
                };
                return View(emptyViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in DonHang Index (HTTP/API hoặc đọc nội dung phản hồi)");

                ViewBag.Search = search;
                ViewBag.FromDate = fromDate;
                ViewBag.ToDate = toDate;
                ViewBag.QuickDate = quickDate;
                ViewBag.IsAuthenticated = User.Identity.IsAuthenticated;

                var emptyViewModel = new
                {
                    HoaDons = new List<HoaDon>(),
                    Pagination = new
                    {
                        CurrentPage = page,
                        TotalPages = 0,
                        TotalCount = 0,
                        PageSize = 10,
                        HasPreviousPage = false,
                        HasNextPage = false
                    }
                };
                return View(emptyViewModel);
            }
        }

        // GET: DonHang/ChiTiet/{id}
        public async Task<IActionResult> ChiTiet(Guid id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"HoaDons/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var hoaDon = await response.Content.ReadFromJsonAsync<HoaDon>();
                    return View(hoaDon);
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể tải thông tin đơn hàng";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception in ChiTiet: {ex.Message}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin đơn hàng";
                return RedirectToAction("Index");
            }
        }

        // GET: DonHang/ChiTietModal/{id} - For modal display
        public async Task<IActionResult> ChiTietModal(Guid id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"HoaDons/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var hoaDon = await response.Content.ReadFromJsonAsync<HoaDon>();

                    var html = $@"
                        <div class='row'>
                            <div class='col-md-6'>
                                <h6>Thông tin đơn hàng</h6>
                                <table class='table table-sm'>
                                    <tr><td><strong>Mã đơn hàng:</strong></td><td>{hoaDon.MaHoaDon}</td></tr>
                                    <tr><td><strong>Ngày đặt:</strong></td><td>{hoaDon.NgayTao.AddHours(7):dd/MM/yyyy HH:mm}</td></tr>
                                    <tr><td><strong>Trạng thái:</strong></td><td>{hoaDon.TrangThai}</td></tr>
                                    <tr><td><strong>Tổng tiền:</strong></td><td>{hoaDon.TongTien:N0} ₫</td></tr>
                                    <tr><td><strong>Tiền giảm:</strong></td><td>{(hoaDon.TienGiam?.ToString("N0") ?? "0")} ₫</td></tr>
                                    {(hoaDon.DiemDaDung > 0 ? $"<tr><td><strong>Điểm đã dùng:</strong></td><td>{hoaDon.DiemDaDung:N0} điểm</td></tr>" : "")}
                                    {(hoaDon.DiemDaDung > 0 ? $"<tr><td><strong>Quy đổi từ điểm:</strong></td><td>-{hoaDon.SoTienGiamTuDiem:N0} ₫ (1 điểm = {hoaDon.TyLeQuyDoiDiem:N0} ₫)</td></tr>" : "")}
                                </table>
                            </div>
                            <div class='col-md-6'>
                                <h6>Địa chỉ giao hàng</h6>
                                <p>{hoaDon.DiaChiGiaoHang ?? "Không có"}</p>
                            </div>
                        </div>
                        <div class='row mt-3'>
                            <div class='col-12'>
                                <h6>Chi tiết sản phẩm</h6>
                                <table class='table table-sm'>
                                    <thead><tr><th>Sản phẩm</th><th>Số lượng</th><th>Đơn giá</th><th>Thành tiền</th></tr></thead>
                                    <tbody>";

                    if (hoaDon.ChiTietHoaDons != null)
                    {
                        foreach (var chiTiet in hoaDon.ChiTietHoaDons)
                        {
                            html += $@"
                                <tr>
                                    <td>{chiTiet.SanPhamChiTiet?.SanPham?.TenSanPham ?? "N/A"}</td>
                                    <td>{chiTiet.SoLuong}</td>
                                    <td>{chiTiet.DonGia:N0} ₫</td>
                                    <td>{chiTiet.ThanhTien:N0} ₫</td>
                                </tr>";
                        }
                    }

                    html += @"
                                    </tbody>
                                </table>
                            </div>
                        </div>";

                    return Content(html, "text/html");
                }
                else
                {
                    return Content("<div class='alert alert-danger'>Không thể tải thông tin đơn hàng</div>", "text/html");
                }
            }
            catch (Exception ex)
            {
                return Content("<div class='alert alert-danger'>Có lỗi xảy ra khi tải thông tin đơn hàng</div>", "text/html");
            }
        }

        // POST: DonHang/HuyDon/{id}
        [HttpPost]
        public async Task<IActionResult> HuyDon(Guid id, [FromBody] HuyDonRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsync($"HoaDons/{id}/trangthai",
                    new StringContent(JsonSerializer.Serialize(new
                    {
                        TrangThai = "Đã hủy",
                        NguoiCapNhat = "Customer",
                        LanCapNhatCuoi = DateTime.UtcNow,
                        LyDoHuyDon = request.LyDoHuyDon
                    }), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "Đã hủy đơn hàng thành công" });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Error canceling order: {response.StatusCode} - {errorContent}");
                    return Json(new { success = false, message = "Không thể hủy đơn hàng" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception canceling order: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi hủy đơn hàng" });
            }
        }

        // GET: DonHang/LichSuDiem
        [HttpGet]
        public async Task<IActionResult> LichSuDiem()
        {
            try
            {
                if (User.Identity?.IsAuthenticated != true)
                {
                    return Unauthorized(new { message = "Vui lòng đăng nhập để xem lịch sử điểm." });
                }

                var customerId = ResolveCurrentCustomerId();
                if (!customerId.HasValue)
                {
                    return BadRequest(new { message = "Không xác định được tài khoản khách hàng." });
                }

                var response = await _httpClient.GetAsync($"KhachHang/{customerId.Value}/lich-su-diem");
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Content(content, "application/json");
                }

                _logger.LogWarning("Không lấy được lịch sử điểm của khách {CustomerId}: {Status} - {Body}",
                    customerId, response.StatusCode, content);
                return StatusCode((int)response.StatusCode, string.IsNullOrWhiteSpace(content)
                    ? "{\"message\":\"Không tải được lịch sử điểm.\"}"
                    : content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception when loading point history");
                return StatusCode(500, new { message = "Có lỗi xảy ra khi tải lịch sử điểm." });
            }
        }
    }

    public class HuyDonRequest
    {
        public string LyDoHuyDon { get; set; }
    }
}
