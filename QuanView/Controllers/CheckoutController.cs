using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using QuanApi.Dtos;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using QuanView.Controllers;
using Microsoft.AspNetCore.Http;
using QuanApi.Dtos;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using QuanView.Models;
using QuanView.Services;

namespace QuanView.Controllers
{
    // SessionExtensions để xử lý session
    public static class SessionExtensions
    {
        public static void SetObjectAsJson(this ISession session, string key, object value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        public static T GetObjectFromJson<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default(T) : JsonSerializer.Deserialize<T>(value);
        }
    }

    //[Authorize]
    public class CheckoutController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IVnPayService _vnPayService;
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(IHttpClientFactory httpClientFactory, IVnPayService vnPayService, ILogger<CheckoutController> logger)
        {
            _httpClient = httpClientFactory.CreateClient("MyApi");
            _vnPayService = vnPayService;
            _logger = logger;
        }

        private bool ValidateVietnamesePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            phoneNumber = phoneNumber.Trim();

            string pattern = @"^(0|\+84)(3[2-9]|5[689]|7[06-9]|8[1-689]|9[0-46-9])[0-9]{7}$";
            
            return Regex.IsMatch(phoneNumber, pattern);
        }

        private string FormatPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return phoneNumber;

            string cleaned = Regex.Replace(phoneNumber, @"[^\d+]", "");
            
            if (cleaned.StartsWith("+84"))
            {
                cleaned = "0" + cleaned.Substring(3);
            }
            
            if (cleaned.StartsWith("84"))
            {
                cleaned = "0" + cleaned.Substring(2);
            }
            
            return cleaned;
        }

        public async Task<IActionResult> Index()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<QuanApi.Data.ChiTietGioHang>>("Cart") ?? new List<QuanApi.Data.ChiTietGioHang>();

            if (!cart.Any())
            {
                return RedirectToAction("Index", "GioHang");
            }

            // Cập nhật thông tin sản phẩm cho từng item
            foreach (var item in cart)
            {
                var responseSpct = await _httpClient.GetAsync($"SanPhamChiTiets/{item.IDSanPhamChiTiet}");
                if (responseSpct.IsSuccessStatusCode)
                {
                    var spct = await responseSpct.Content.ReadFromJsonAsync<SanPhamChiTietDto>();
                    if (spct != null)
                    {
                        item.GiaBan = spct.price;
                        item.SanPhamChiTiet = new SanPhamChiTiet
                        {
                            SanPham = new SanPham
                            {
                                TenSanPham = spct.TenSanPham
                            },
                            GiaBan = spct.price,
                            AnhSanPhams = new List<AnhSanPham>
                            {
                                new AnhSanPham
                                {
                                    UrlAnh = spct.AnhDaiDien ?? "/img/default-product.jpg",
                                    LaAnhChinh = true
                                }
                            }
                        };
                    }
                }
            }

            // Tính tổng tiền
            var tongTien = cart.Sum(item => item.GiaBan * item.SoLuong);
            
            ViewBag.CartItems = cart;
            ViewBag.TongTien = tongTien;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ProcessOrder([FromBody] CheckoutDto checkoutData)
        {
            try
            {
                // Validate số điện thoại người nhận
                if (string.IsNullOrWhiteSpace(checkoutData.SoDienThoaiNguoiNhan))
                {
                    return Json(new { success = false, message = "Số điện thoại người nhận không được để trống" });
                }

                string formattedPhone = FormatPhoneNumber(checkoutData.SoDienThoaiNguoiNhan);
                
                if (!ValidateVietnamesePhoneNumber(formattedPhone))
                {
                    return Json(new { success = false, message = "Số điện thoại người nhận không hợp lệ" });
                }

                // Cập nhật số điện thoại đã được format
                checkoutData.SoDienThoaiNguoiNhan = formattedPhone;

                // Lấy giỏ hàng từ session
                var cart = HttpContext.Session.GetObjectFromJson<List<QuanApi.Data.ChiTietGioHang>>("Cart") ?? new List<QuanApi.Data.ChiTietGioHang>();

                if (!cart.Any())
                {
                    return Json(new { success = false, message = "Giỏ hàng trống" });
                }

                // Cập nhật lại thông tin sản phẩm và giá trước khi tạo hóa đơn
                foreach (var item in cart)
                {
                    var responseSpct = await _httpClient.GetAsync($"SanPhamChiTiets/{item.IDSanPhamChiTiet}");
                    if (responseSpct.IsSuccessStatusCode)
                    {
                        var spct = await responseSpct.Content.ReadFromJsonAsync<SanPhamChiTietDto>();
                        if (spct != null && spct.price > 0)
                        {
                            item.GiaBan = spct.price;
                        }
                        else
                        {
                            return Json(new { success = false, message = $"Không thể lấy giá sản phẩm cho ID: {item.IDSanPhamChiTiet}" });
                        }
                    }
                    else
                    {
                        return Json(new { success = false, message = $"Không thể lấy thông tin sản phẩm cho ID: {item.IDSanPhamChiTiet}" });
                    }
                }

                // Tạo danh sách chi tiết hóa đơn với giá đã được cập nhật
                var chiTietHoaDons = cart.Select(item => new
                {
                    idSanPhamChiTiet = item.IDSanPhamChiTiet,
                    soLuong = item.SoLuong,
                    donGia = item.GiaBan,
                    thanhTien = Math.Round(item.GiaBan * item.SoLuong, 2)
                }).ToList();

                // Log để debug
                Console.WriteLine($"Số lượng chi tiết hóa đơn: {chiTietHoaDons.Count()}");
                foreach (var ct in chiTietHoaDons)
                {
                    Console.WriteLine($"SPCT ID: {ct.idSanPhamChiTiet}, SL: {ct.soLuong}, Đơn giá: {ct.donGia}, Thành tiền: {ct.thanhTien}");
                }

                // Lấy KhachHangId từ claims nếu user đã đăng nhập
                Guid? khachHangId = null;
                var customerIdClaim = User.FindFirst("custom:id_khachhang");
                if (customerIdClaim != null && Guid.TryParse(customerIdClaim.Value, out var parsedCustomerId))
                {
                    khachHangId = parsedCustomerId;
                }
                
                // Xử lý phiếu giảm giá nếu có
                Guid? phieuGiamGiaId = null;
                if (!string.IsNullOrEmpty(checkoutData.MaGiamGia))
                {
                    try
                    {
                        // Tìm phiếu giảm giá theo mã
                        var responsePhieu = await _httpClient.GetAsync($"PhieuGiamGias/kiem-tra?code={Uri.EscapeDataString(checkoutData.MaGiamGia)}");
                        if (responsePhieu.IsSuccessStatusCode)
                        {
                            var phieuResult = await responsePhieu.Content.ReadFromJsonAsync<PhieuGiamGiaResponse>();
                            if (phieuResult != null && phieuResult.Success && phieuResult.IdPhieuGiamGia.HasValue)
                            {
                                phieuGiamGiaId = phieuResult.IdPhieuGiamGia.Value;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Lỗi khi xử lý phiếu giảm giá: {ex.Message}");
                    }
                }
                
                Console.WriteLine($"KhachHangId from claims: {khachHangId}");
                Console.WriteLine($"PhieuGiamGiaId: {phieuGiamGiaId}");
                Console.WriteLine($"MaGiamGia: {checkoutData.MaGiamGia}");
                
                // Tính phí vận chuyển động dựa trên API Shipping nếu có province/district
                try
                {
                    var orderValue = chiTietHoaDons.Sum(x => x.thanhTien);
                    if (!string.IsNullOrWhiteSpace(checkoutData.Province))
                    {
                        var calcRequest = new
                        {
                            Province = checkoutData.Province,
                            District = checkoutData.District,
                            OrderValue = orderValue
                        };
                        var shippingResp = await _httpClient.PostAsJsonAsync("shipping/calculate", calcRequest);
                        if (shippingResp.IsSuccessStatusCode)
                        {
                            var shippingInfo = await shippingResp.Content.ReadFromJsonAsync<ShippingInfoDto>();
                            if (shippingInfo != null)
                            {
                                checkoutData.PhiVanChuyen = shippingInfo.FinalFee;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Không tính được phí vận chuyển động, dùng giá trị gửi từ client nếu có");
                }

                // Kiểm tra phương thức thanh toán - nếu là "chuyển khoản" thì chuyển hướng đến VNPay
                var paymentMethodResponse = await _httpClient.GetAsync($"PhuongThucThanhToans/{checkoutData.PhuongThucThanhToanId}");
                if (paymentMethodResponse.IsSuccessStatusCode)
                {
                    var paymentMethod = await paymentMethodResponse.Content.ReadFromJsonAsync<PhuongThucThanhToan>();
                    if (paymentMethod != null && paymentMethod.TenPhuongThuc.ToLower().Contains("chuyển khoản"))
                    {
                        // Lưu thông tin đơn hàng vào session với format CheckoutDto để deserialize đúng
                        var orderInfo = new CheckoutDto
                        {
                            KhachHangId = khachHangId,
                            PhuongThucThanhToanId = checkoutData.PhuongThucThanhToanId,
                            TongTien = checkoutData.TongTien,
                            TienGiam = checkoutData.TienGiam ?? 0,
                            PhiVanChuyen = checkoutData.PhiVanChuyen,
                            TenNguoiNhan = checkoutData.TenNguoiNhan,
                            SoDienThoaiNguoiNhan = checkoutData.SoDienThoaiNguoiNhan,
                            DiaChiGiaoHang = checkoutData.DiaChiGiaoHang,
                            GhiChu = checkoutData.GhiChu,
                            PhieuGiamGiaId = phieuGiamGiaId,
                            Province = checkoutData.Province,
                            District = checkoutData.District,
                            MaGiamGia = checkoutData.MaGiamGia
                        };
                        
                        HttpContext.Session.SetObjectAsJson("PendingOrder", orderInfo);

                        // Tạo thông tin thanh toán VNPay
                        var paymentInfo = new PaymentInformationModel
                        {
                            OrderType = "billpayment",
                            Amount = (double)checkoutData.TongTien,
                            OrderDescription = $"Thanh toan don hang {checkoutData.TenNguoiNhan}",
                            Name = checkoutData.TenNguoiNhan
                        };

                        // Debug: Log số tiền trước khi gửi VNPay
                        _logger.LogInformation($"VNPay Payment - Amount: {paymentInfo.Amount}, TongTien: {checkoutData.TongTien}");

                        // Tạo URL thanh toán VNPay
                        var paymentUrl = _vnPayService.CreatePaymentUrl(paymentInfo, HttpContext);
                        
                        return Json(new { 
                            success = true, 
                            redirectToVnPay = true,
                            paymentUrl = paymentUrl,
                            message = "Chuyển hướng đến VNPay để thanh toán" 
                        });
                    }
                }

                // Tạo dữ liệu hóa đơn cho thanh toán thường (tiền mặt, COD)
                var hoaDonData = new
                {
                    khachHangId = khachHangId, // Sử dụng từ claims thay vì checkoutData
                    nhanVienId = (Guid?)null,
                    phieuGiamGiaId = phieuGiamGiaId, // Sử dụng từ API thay vì checkoutData
                    phuongThucThanhToanId = checkoutData.PhuongThucThanhToanId,
                    tongTien = checkoutData.TongTien,
                    tienGiam = checkoutData.TienGiam,
                    phiVanChuyen = checkoutData.PhiVanChuyen, // Dùng phí đã tính từ API nếu có
                    tenNguoiNhan = checkoutData.TenNguoiNhan,
                    soDienThoaiNguoiNhan = checkoutData.SoDienThoaiNguoiNhan,
                    diaChiGiaoHang = checkoutData.DiaChiGiaoHang,
                    ghiChu = checkoutData.GhiChu,
                    chiTietHoaDons = chiTietHoaDons
                };

                // Xử lý đặt hàng
                var response = await _httpClient.PostAsJsonAsync("HoaDons", hoaDonData);

                if (response.IsSuccessStatusCode)
                {
                    // Lấy thông tin hóa đơn từ response
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API Response: {responseContent}");
                    
                    string maHoaDon = $"HD_{DateTime.Now:yyyyMMddHHmmss}"; // Fallback
                    
                    try
                    {
                        // Thử parse response như một object thông thường
                        var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        
                        // Kiểm tra xem có property "maHoaDon" không
                        if (responseData.TryGetProperty("maHoaDon", out var maHoaDonElement))
                        {
                            maHoaDon = maHoaDonElement.GetString();
                        }
                        else if (responseData.TryGetProperty("MaHoaDon", out var MaHoaDonElement))
                        {
                            maHoaDon = MaHoaDonElement.GetString();
                        }
                        else
                        {
                            // Thử parse như HoaDon object
                            var hoaDonResponse = JsonSerializer.Deserialize<HoaDon>(responseContent);
                            maHoaDon = hoaDonResponse?.MaHoaDon ?? maHoaDon;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing response: {ex.Message}");
                        // Sử dụng fallback
                    }
                    
                    // Chuẩn bị dữ liệu cho trang Success
                    ViewBag.OrderCode = maHoaDon;
                    ViewBag.OrderTotal = checkoutData.TongTien;
                    ViewBag.PaymentMethod = "Thanh toán khi nhận hàng";
                    ViewBag.OrderDate = DateTime.Now;
                    ViewBag.CustomerName = checkoutData.TenNguoiNhan;
                    ViewBag.CustomerPhone = checkoutData.SoDienThoaiNguoiNhan;
                    ViewBag.CustomerAddress = checkoutData.DiaChiGiaoHang;
                    ViewBag.ShippingFee = checkoutData.PhiVanChuyen;
                    
                    HttpContext.Session.Remove("Cart");
                    
                    return Json(new { 
                        success = true, 
                        redirect = true,
                        redirectUrl = Url.Action("Success", "Checkout", new { 
                            orderCode = maHoaDon,
                            total = checkoutData.TongTien,
                            paymentMethod = "Thanh toán khi nhận hàng",
                            customerName = checkoutData.TenNguoiNhan,
                            customerPhone = checkoutData.SoDienThoaiNguoiNhan,
                            customerAddress = checkoutData.DiaChiGiaoHang,
                            shippingFee = checkoutData.PhiVanChuyen
                        }),
                        message = $"Đặt hàng thành công! Mã đơn hàng của bạn là: {maHoaDon}" 
                    });
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, message = $"Lỗi: {error}" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // Proxy tính phí vận chuyển giống bán hàng tại quầy
        [HttpPost]
        public async Task<IActionResult> CalculateShipping([FromBody] object shippingData)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("shipping/calculate", shippingData);
                var result = await response.Content.ReadAsStringAsync();
                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPaymentMethods()
        {
            try
            {
                var response = await _httpClient.GetAsync("PhuongThucThanhToans");
                if (response.IsSuccessStatusCode)
                {
                    var methods = await response.Content.ReadFromJsonAsync<List<PhuongThucThanhToan>>();
                    return Json(methods);
                }
                return Json(new List<PhuongThucThanhToan>());
            }
            catch
            {
                return Json(new List<PhuongThucThanhToan>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> KiemTraMaGiamGia(string code)
        {
            try
            {
                var response = await _httpClient.GetAsync($"PhieuGiamGias/kiem-tra?code={Uri.EscapeDataString(code)}");
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<object>();
                    return Json(result);
                }
                return Json(new { success = false, message = "Mã giảm giá không hợp lệ" });
            }
            catch
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi kiểm tra mã giảm giá" });
            }
        }

        [HttpPost]
        public IActionResult ClearCart()
        {
            HttpContext.Session.Remove("Cart");
            return Json(new { success = true });
        }

        // GET: Trang thành công
        public IActionResult Success(string orderCode, decimal? total = null, string paymentMethod = null, 
            string customerName = null, string customerPhone = null, string customerAddress = null, 
            decimal? shippingFee = null)
        {
            ViewBag.OrderCode = orderCode;
            ViewBag.OrderTotal = total ?? 0;
            ViewBag.PaymentMethod = paymentMethod ?? "Thanh toán khi nhận hàng";
            ViewBag.OrderDate = DateTime.Now;
            ViewBag.CustomerName = customerName;
            ViewBag.CustomerPhone = customerPhone;
            ViewBag.CustomerAddress = customerAddress;
            ViewBag.ShippingFee = shippingFee ?? 0;
            return View();
        }

        // GET: Test endpoint để kiểm tra
        [HttpGet]
        public IActionResult TestOrderCode()
        {
            var testOrderCode = $"HD_{DateTime.Now:yyyyMMddHHmmss}";
            return Json(new { 
                success = true, 
                message = $"Đặt hàng thành công! Mã đơn hàng của bạn là: {testOrderCode}",
                orderCode = testOrderCode
            });
        }

        [HttpGet]
        public IActionResult TestSession()
        {
            var pendingOrder = HttpContext.Session.GetString("PendingOrder");
            return Json(new { 
                hasPendingOrder = !string.IsNullOrEmpty(pendingOrder),
                pendingOrderData = pendingOrder
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetCurrentCustomer()
        {
            try
            {
                var customerId = HttpContext.Session.GetString("CustomerId");
                if (string.IsNullOrEmpty(customerId))
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin khách hàng" });
                }

                var response = await _httpClient.GetAsync($"KhachHang/{customerId}");
                if (response.IsSuccessStatusCode)
                {
                    var customer = await response.Content.ReadFromJsonAsync<object>();
                    return Json(new { success = true, customer = customer });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể lấy thông tin khách hàng" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomerAddresses()
        {
            try
            {
                var customerId = HttpContext.Session.GetString("CustomerId");
                string effectiveCustomerId = customerId;
                if (string.IsNullOrEmpty(effectiveCustomerId))
                {
                    var claim = User.FindFirst("custom:id_khachhang")?.Value;
                    if (!string.IsNullOrEmpty(claim)) effectiveCustomerId = claim;
                }

                if (string.IsNullOrEmpty(effectiveCustomerId))
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin khách hàng" });
                }

                var response = await _httpClient.GetAsync($"KhachHang/{effectiveCustomerId}/addresses");
                if (response.IsSuccessStatusCode)
                {
                    var addresses = await response.Content.ReadFromJsonAsync<List<AddressDto>>();
                    return Json(new { success = true, addresses = addresses ?? new List<AddressDto>() });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể lấy danh sách địa chỉ" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // Thêm action để lấy địa chỉ mặc định của khách hàng
        [HttpGet]
        public async Task<IActionResult> GetDefaultAddress()
        {
            try
            {
                var customerId = HttpContext.Session.GetString("CustomerId");
                if (string.IsNullOrEmpty(customerId))
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin khách hàng" });
                }

                // Sử dụng API endpoint mới để lấy địa chỉ mặc định
                var response = await _httpClient.GetAsync($"KhachHang/{customerId}/default-address");
                if (response.IsSuccessStatusCode)
                {
                    var address = await response.Content.ReadFromJsonAsync<AddressDto>();
                    return Json(new { success = true, address = address });
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return Json(new { success = false, message = "Không tìm thấy địa chỉ mặc định" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể lấy địa chỉ mặc định" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // Thêm action để lấy dữ liệu giỏ hàng cho checkout
        [HttpGet]
        public async Task<IActionResult> GetCheckoutCart()
        {
            try
            {
                var customerIdClaim = User.FindFirst("custom:id_khachhang");
                if (customerIdClaim == null || !Guid.TryParse(customerIdClaim.Value, out var customerId))
                {
                    // Khách hàng - trả về giỏ hàng session với thông tin sản phẩm đầy đủ
                    var cart = HttpContext.Session.GetObjectFromJson<List<QuanApi.Data.ChiTietGioHang>>("Cart") ?? new List<QuanApi.Data.ChiTietGioHang>();
                    
                    // Cập nhật thông tin sản phẩm cho từng item (giống như trong GioHangController)
                    foreach (var item in cart)
                    {
                        var responseSpct = await _httpClient.GetAsync($"SanPhamChiTiets/{item.IDSanPhamChiTiet}");
                        if (responseSpct.IsSuccessStatusCode)
                        {
                            var spct = await responseSpct.Content.ReadFromJsonAsync<SanPhamChiTietDto>();
                            if (spct != null)
                            {
                                item.GiaBan = spct.price;
                                item.SanPhamChiTiet = new SanPhamChiTiet
                                {
                                    SanPham = new SanPham
                                    {
                                        TenSanPham = spct.TenSanPham
                                    },
                                    GiaBan = spct.price,
                                    AnhSanPhams = new List<AnhSanPham>
                                    {
                                        new AnhSanPham
                                        {
                                            UrlAnh = spct.AnhDaiDien ?? "/img/default-product.jpg",
                                            LaAnhChinh = true
                                        }
                                    }
                                };
                            }
                        }
                    }
                    
                    return Json(new { success = true, chiTietGioHangs = cart });
                }

                // Người dùng đã đăng nhập - trả về giỏ hàng từ database
                var response = await _httpClient.GetAsync($"GioHangs/getbyuser?iduser={customerId}");
                if (response.IsSuccessStatusCode)
                {
                    var gioHang = await response.Content.ReadFromJsonAsync<QuanApi.Data.GioHang>();
                    var chiTietGioHangs = gioHang?.ChiTietGioHangs ?? new List<QuanApi.Data.ChiTietGioHang>();
                    
                    // Cập nhật thông tin sản phẩm cho từng item
                    foreach (var item in chiTietGioHangs)
                    {
                        var responseSpct = await _httpClient.GetAsync($"SanPhamChiTiets/{item.IDSanPhamChiTiet}");
                        if (responseSpct.IsSuccessStatusCode)
                        {
                            var spct = await responseSpct.Content.ReadFromJsonAsync<SanPhamChiTietDto>();
                            if (spct != null)
                            {
                                item.GiaBan = spct.price;
                                item.SanPhamChiTiet = new SanPhamChiTiet
                                {
                                    SanPham = new SanPham
                                    {
                                        TenSanPham = spct.TenSanPham
                                    },
                                    GiaBan = spct.price,
                                    AnhSanPhams = new List<AnhSanPham>
                                    {
                                        new AnhSanPham
                                        {
                                            UrlAnh = spct.AnhDaiDien ?? "/img/default-product.jpg",
                                            LaAnhChinh = true
                                        }
                                    }
                                };
                            }
                        }
                    }
                    
                    return Json(new { success = true, chiTietGioHangs = chiTietGioHangs });
                }

                return Json(new { success = true, chiTietGioHangs = new List<QuanApi.Data.ChiTietGioHang>() });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // Test endpoint để kiểm tra session cart
        [HttpGet]
        public IActionResult TestSessionCart()
        {
            try
            {
                var cart = HttpContext.Session.GetObjectFromJson<List<QuanApi.Data.ChiTietGioHang>>("Cart");
                return Json(new { 
                    success = true, 
                    cartCount = cart?.Count ?? 0,
                    cart = cart,
                    sessionKeys = HttpContext.Session.Keys.ToList()
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomerVouchers()
        {
            try
            {
                // Lấy phiếu giảm giá công khai từ KhachHangPhieuGiamsController
                var response = await _httpClient.GetAsync("KhachHangPhieuGiam/phieu-giam-gia-cong-khai");
                if (response.IsSuccessStatusCode)
                {
                    var vouchers = await response.Content.ReadFromJsonAsync<object>();
                    return Ok(vouchers);
                }
                return StatusCode((int)response.StatusCode, "Lỗi khi lấy danh sách phiếu giảm giá công khai");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomerPersonalVouchers()
        {
            try
            {
                // Lấy ID khách hàng từ claims
                var customerIdClaim = User.FindFirst("custom:id_khachhang")?.Value;
                if (string.IsNullOrEmpty(customerIdClaim) || !Guid.TryParse(customerIdClaim, out Guid customerId))
                {
                    return Ok(new List<object>()); // Trả về danh sách rỗng nếu chưa đăng nhập
                }

                // Lấy phiếu giảm giá riêng của khách hàng
                var response = await _httpClient.GetAsync($"KhachHangPhieuGiam/phieu-giam-gia-cua-khach-hang/{customerId}");
                if (response.IsSuccessStatusCode)
                {
                    var vouchers = await response.Content.ReadFromJsonAsync<object>();
                    return Ok(vouchers);
                }
                return StatusCode((int)response.StatusCode, "Lỗi khi lấy danh sách phiếu giảm giá của khách hàng");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi: {ex.Message}");
            }
        }

        // Tạo địa chỉ mới cho khách hàng
        [HttpPost]
        public async Task<IActionResult> TaoDiaChi([FromBody] TaoDiaChiDto dto)
        {
            try
            {
                // Kiểm tra user có đăng nhập không
                var customerIdClaim = User.FindFirst("custom:id_khachhang")?.Value;
                if (string.IsNullOrEmpty(customerIdClaim) || !Guid.TryParse(customerIdClaim, out Guid customerId))
                {
                    return BadRequest("Vui lòng đăng nhập để lưu địa chỉ!");
                }

                // Gán ID khách hàng từ claims
                dto.IDKhachHang = customerId;

                // Gọi API tạo địa chỉ
                var response = await _httpClient.PostAsJsonAsync("BanHangTaiQuay/tao-dia-chi", dto);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<object>();
                    return Ok(result);
                }
                var errorContent = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, errorContent);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi: {ex.Message}");
            }
        }

        // Xóa địa chỉ đã lưu (gọi API backend, tránh CORS)
        [HttpDelete]
        public async Task<IActionResult> DeleteAddress(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                    return BadRequest(new { success = false, message = "ID địa chỉ không hợp lệ" });

                var response = await _httpClient.DeleteAsync($"BanHangTaiQuay/xoa-dia-chi/{id}");
                if (response.IsSuccessStatusCode)
                {
                    return Json(new { success = true, message = "Xóa địa chỉ thành công" });
                }

                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { success = false, message = string.IsNullOrWhiteSpace(error) ? "Xóa địa chỉ thất bại" : error });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // VNPay callback handler
        [HttpGet]
        public async Task<IActionResult> PaymentCallbackVnpay()
        {
            try
            {
                // Log toàn bộ query parameters để debug
                _logger.LogInformation($"VNPay Callback - Query: {string.Join("&", Request.Query.Select(x => $"{x.Key}={x.Value}"))}");
                
                // Xử lý response từ VNPay
                var response = _vnPayService.PaymentExecute(Request.Query);
                
                _logger.LogInformation($"VNPay Response - Success: {response.Success}, ResponseCode: {response.VnPayResponseCode}");

                if (response.Success && response.VnPayResponseCode == "00")
                {
                    // Thanh toán thành công, lấy thông tin đơn hàng từ session
                    var orderInfoJson = HttpContext.Session.GetString("PendingOrder");
                    _logger.LogInformation($"VNPay Success - PendingOrder from session: {orderInfoJson}");
                    
                    if (!string.IsNullOrEmpty(orderInfoJson))
                    {
                        // Parse CheckoutDto và tạo lại format cho API HoaDons
                        var checkoutInfo = JsonSerializer.Deserialize<CheckoutDto>(orderInfoJson);
                        
                        // Lấy chi tiết hóa đơn từ giỏ hàng
                        var cart = HttpContext.Session.GetObjectFromJson<List<QuanApi.Data.ChiTietGioHang>>("Cart") ?? new List<QuanApi.Data.ChiTietGioHang>();
                        var chiTietHoaDons = cart.Select(item => new
                        {
                            idSanPhamChiTiet = item.IDSanPhamChiTiet,
                            soLuong = item.SoLuong,
                            donGia = item.GiaBan,
                            thanhTien = Math.Round(item.GiaBan * item.SoLuong, 2)
                        }).ToList();
                        
                        // Tạo dữ liệu hóa đơn với format đúng cho API
                        var hoaDonData = new
                        {
                            khachHangId = checkoutInfo.KhachHangId,
                            nhanVienId = (Guid?)null,
                            phieuGiamGiaId = checkoutInfo.PhieuGiamGiaId,
                            phuongThucThanhToanId = checkoutInfo.PhuongThucThanhToanId,
                            tongTien = checkoutInfo.TongTien,
                            tienGiam = checkoutInfo.TienGiam,
                            phiVanChuyen = checkoutInfo.PhiVanChuyen,
                            tenNguoiNhan = checkoutInfo.TenNguoiNhan,
                            soDienThoaiNguoiNhan = checkoutInfo.SoDienThoaiNguoiNhan,
                            diaChiGiaoHang = checkoutInfo.DiaChiGiaoHang,
                            ghiChu = checkoutInfo.GhiChu,
                            chiTietHoaDons = chiTietHoaDons
                        };
                        
                        var content = new StringContent(JsonSerializer.Serialize(hoaDonData), Encoding.UTF8, "application/json");
                        var hoaDonResponse = await _httpClient.PostAsync("HoaDons", content);
                        
                        _logger.LogInformation($"VNPay Success - API Response Status: {hoaDonResponse.StatusCode}");
                        
                        if (hoaDonResponse.IsSuccessStatusCode)
                        {
                            // Lấy mã hóa đơn từ response
                            var responseContent = await hoaDonResponse.Content.ReadAsStringAsync();
                            using (var doc = JsonDocument.Parse(responseContent))
                            {
                                var maHoaDon = doc.RootElement.GetProperty("maHoaDon").GetString();
                                
                                // Chuẩn bị dữ liệu cho trang Success
                                ViewBag.OrderCode = maHoaDon;
                                ViewBag.OrderTotal = checkoutInfo.TongTien;
                                ViewBag.PaymentMethod = "Thanh toán VNPay";
                                ViewBag.OrderDate = DateTime.Now;
                                ViewBag.CustomerName = checkoutInfo.TenNguoiNhan;
                                ViewBag.CustomerPhone = checkoutInfo.SoDienThoaiNguoiNhan;
                                ViewBag.CustomerAddress = checkoutInfo.DiaChiGiaoHang;
                                ViewBag.ShippingFee = checkoutInfo.PhiVanChuyen;
                                
                                // Xóa giỏ hàng và thông tin đơn hàng tạm
                                HttpContext.Session.Remove("Cart");
                                HttpContext.Session.Remove("PendingOrder");
                                
                                // Chuyển hướng đến trang Success
                                return View("Success");
                            }
                        }
                        else
                        {
                            var errorContent = await hoaDonResponse.Content.ReadAsStringAsync();
                            _logger.LogError($"API Error: {errorContent}");
                            TempData["ErrorMessage"] = "Có lỗi xảy ra khi tạo đơn hàng.";
                            return RedirectToAction("Index");
                        }
                    }
                    else
                    {
                        // Không tìm thấy thông tin đơn hàng
                        TempData["ErrorMessage"] = "Không tìm thấy thông tin đơn hàng. Vui lòng thử lại.";
                        return RedirectToAction("Index", "GioHang");
                    }
                }
                else
                {
                    // Thanh toán thất bại hoặc bị hủy
                    string errorMessage = "Thanh toán thất bại.";
                    bool isUserCancelled = false;
                    
                    switch (response.VnPayResponseCode)
                    {
                        case "24":
                            errorMessage = "Bạn đã hủy giao dịch thanh toán.";
                            isUserCancelled = true;
                            break;
                        case "51":
                            errorMessage = "Tài khoản không đủ số dư để thực hiện giao dịch.";
                            break;
                        case "65":
                            errorMessage = "Tài khoản đã vượt quá hạn mức giao dịch trong ngày.";
                            break;
                        default:
                            errorMessage = $"Thanh toán thất bại. Mã lỗi: {response.VnPayResponseCode}";
                            break;
                    }
                    
                    // Nếu user hủy giao dịch, chỉ hiển thị thông báo nhẹ nhàng
                    if (isUserCancelled)
                    {
                        TempData["InfoMessage"] = errorMessage;
                    }
                    else
                    {
                        TempData["ErrorMessage"] = errorMessage;
                    }
                    
                    // Xóa thông tin đơn hàng tạm nếu user hủy
                    if (isUserCancelled)
                    {
                        HttpContext.Session.Remove("PendingOrder");
                    }
                    
                    return RedirectToAction("Index", "GioHang");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PaymentCallbackVnpay");
                TempData["ErrorMessage"] = "Có lỗi xảy ra trong quá trình xử lý thanh toán.";
                return RedirectToAction("Index");
            }
        }

    }

    public class CheckoutDto
    {
        public Guid? KhachHangId { get; set; }
        public string TenNguoiNhan { get; set; }
        public string SoDienThoaiNguoiNhan { get; set; }
        public string DiaChiGiaoHang { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
        public Guid PhuongThucThanhToanId { get; set; }
        public Guid? PhieuGiamGiaId { get; set; }
        public decimal TongTien { get; set; }
        public decimal? TienGiam { get; set; }
        public string GhiChu { get; set; }
        public string MaGiamGia { get; set; } 
        public decimal PhiVanChuyen { get; set; } = 50000; 
    }

    public class PhieuGiamGiaResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Guid? IdPhieuGiamGia { get; set; }
        public decimal? GiaTriGiam { get; set; }
        public decimal? GiaTriGiamToiDa { get; set; }
        public decimal? DonToiThieu { get; set; }
    }

    public class AddressDto
    {
        public Guid IDDiaChi { get; set; }
        public string MaDiaChi { get; set; }
        public string DiaChiChiTiet { get; set; }
        public Guid IDKhachHang { get; set; }
        public bool LaMacDinh { get; set; }
        public string TenNguoiNhan { get; set; }
        public string SdtNguoiNhan { get; set; }
        public DateTime NgayTao { get; set; }
        public string NguoiTao { get; set; }
        public DateTime? LanCapNhatCuoi { get; set; }
        public string NguoiCapNhat { get; set; }
        public bool TrangThai { get; set; }
    }
}