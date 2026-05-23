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
using QuanApi.Services;
using System;

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

        private Guid? ResolveCurrentCustomerId()
        {
            var customerIdClaim = User.FindFirst("custom:id_khachhang");
            if (customerIdClaim != null && Guid.TryParse(customerIdClaim.Value, out var parsedCustomerId))
            {
                return parsedCustomerId;
            }

            return null;
        }

        private List<Guid> GetSelectedCartItemIds()
        {
            return HttpContext.Session.GetObjectFromJson<List<Guid>>("SelectedCartItemIds") ?? new List<Guid>();
        }

        private List<ChiTietGioHang> FilterCartBySelectedItems(IEnumerable<ChiTietGioHang> cart, List<Guid> selectedItemIds)
        {
            var source = cart?.ToList() ?? new List<ChiTietGioHang>();
            if (selectedItemIds == null || selectedItemIds.Count == 0)
            {
                return source;
            }

            var selectedLookup = selectedItemIds.ToHashSet();
            return source.Where(item => selectedLookup.Contains(item.IDChiTietGioHang)).ToList();
        }

        private async Task<List<ChiTietGioHang>> GetCurrentCartAsync(Guid? customerId = null)
        {
            var effectiveCustomerId = customerId ?? ResolveCurrentCustomerId();
            if (!effectiveCustomerId.HasValue)
            {
                return HttpContext.Session.GetObjectFromJson<List<QuanApi.Data.ChiTietGioHang>>("Cart")
                    ?? new List<QuanApi.Data.ChiTietGioHang>();
            }

            var response = await _httpClient.GetAsync($"GioHangs/user/{effectiveCustomerId.Value}");
            if (!response.IsSuccessStatusCode)
            {
                return new List<QuanApi.Data.ChiTietGioHang>();
            }

            var gioHang = await response.Content.ReadFromJsonAsync<QuanApi.Data.GioHang>();
            return gioHang?.ChiTietGioHangs?.ToList() ?? new List<QuanApi.Data.ChiTietGioHang>();
        }

        private async Task EnrichCartItemDetailsAsync(List<ChiTietGioHang> cart)
        {
            foreach (var item in cart)
            {
                var responseSpct = await _httpClient.GetAsync($"SanPhamChiTiets/{item.IDSanPhamChiTiet}");
                if (!responseSpct.IsSuccessStatusCode)
                {
                    continue;
                }

                var spct = await responseSpct.Content.ReadFromJsonAsync<SanPhamChiTietDto>();
                if (spct == null)
                {
                    continue;
                }

                item.GiaBan = spct.price;
                item.SanPhamChiTiet = new SanPhamChiTiet
                {
                    SanPham = new SanPham
                    {
                        TenSanPham = spct.TenSanPham
                    },
                    GiaBan = spct.price,
                    SoLuong = spct.SoLuongKhaDung,
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

        private async Task RemovePurchasedItemsFromCurrentCartAsync(List<Guid> purchasedItemIds, Guid? customerId = null)
        {
            if (purchasedItemIds == null || purchasedItemIds.Count == 0)
            {
                return;
            }

            var effectiveCustomerId = customerId ?? ResolveCurrentCustomerId();
            if (effectiveCustomerId.HasValue)
            {
                var dbCart = await GetCurrentCartAsync(effectiveCustomerId.Value);
                var purchasedLookup = purchasedItemIds.ToHashSet();
                var toDelete = dbCart
                    .Where(item => purchasedLookup.Contains(item.IDChiTietGioHang))
                    .Select(item => item.IDChiTietGioHang)
                    .Distinct()
                    .ToList();

                foreach (var idChiTiet in toDelete)
                {
                    await _httpClient.DeleteAsync($"GioHangs/item/{idChiTiet}");
                }

                return;
            }

            var cart = HttpContext.Session.GetObjectFromJson<List<QuanApi.Data.ChiTietGioHang>>("Cart") ?? new List<QuanApi.Data.ChiTietGioHang>();
            if (cart.Count == 0)
            {
                return;
            }

            var sessionLookup = purchasedItemIds.ToHashSet();
            cart = cart.Where(item => !sessionLookup.Contains(item.IDChiTietGioHang)).ToList();
            HttpContext.Session.SetObjectAsJson("Cart", cart);
        }

        private async Task<PhieuGiamGiaResponse?> KiemTraPhieuGiamGiaHopLeAsync(string maGiamGia)
        {
            if (string.IsNullOrWhiteSpace(maGiamGia))
            {
                return null;
            }

            var code = Uri.EscapeDataString(maGiamGia.Trim());
            var endpoints = new[]
            {
                $"PhieuGiamGia/kiem-tra?code={code}",
                $"PhieuGiamGias/kiem-tra?code={code}"
            };

            foreach (var endpoint in endpoints)
            {
                try
                {
                    var response = await _httpClient.GetAsync(endpoint);
                    if (!response.IsSuccessStatusCode)
                    {
                        continue;
                    }

                    var result = await response.Content.ReadFromJsonAsync<PhieuGiamGiaResponse>();
                    if (result != null)
                    {
                        return result;
                    }
                }
                catch
                {
                    // Thử endpoint dự phòng
                }
            }

            return null;
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
            var customerId = ResolveCurrentCustomerId();
            var cart = await GetCurrentCartAsync(customerId);
            var selectedItemIds = GetSelectedCartItemIds();

            cart = FilterCartBySelectedItems(cart, selectedItemIds);

            if (!cart.Any())
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm đã chọn để thanh toán. Vui lòng chọn lại.";
                HttpContext.Session.Remove("SelectedCartItemIds");
                return RedirectToAction("Index", "GioHang");
            }

            await EnrichCartItemDetailsAsync(cart);

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
                var selectedItemIds = (checkoutData.SelectedCartItemIds ?? new List<Guid>())
                    .Where(id => id != Guid.Empty)
                    .Distinct()
                    .ToList();

                if (!selectedItemIds.Any())
                {
                    selectedItemIds = GetSelectedCartItemIds();
                }

                if (!selectedItemIds.Any())
                {
                    return Json(new { success = false, message = "Không xác định được sản phẩm đã chọn. Vui lòng quay lại giỏ hàng và chọn lại." });
                }

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

                if (string.IsNullOrWhiteSpace(checkoutData.DiaChiGiaoHang))
                {
                    return Json(new { success = false, message = "Vui lòng nhập đầy đủ địa chỉ giao hàng trước khi thanh toán" });
                }

                var hasProvince = !string.IsNullOrWhiteSpace(checkoutData.Province);
                var hasDistrict = !string.IsNullOrWhiteSpace(checkoutData.District);
                var hasDistrictId = checkoutData.ToDistrictId.GetValueOrDefault() > 0;
                var hasWardCode = !string.IsNullOrWhiteSpace(checkoutData.ToWardCode);

                if (!hasProvince || !hasDistrict || !hasDistrictId || !hasWardCode)
                {
                    return Json(new { success = false, message = "Vui lòng chọn đầy đủ Tỉnh/Thành, Quận/Huyện, Phường/Xã trước khi thanh toán" });
                }

                // Lấy giỏ hàng theo trạng thái đăng nhập (DB cho user, session cho guest)
                var cart = await GetCurrentCartAsync();
                cart = FilterCartBySelectedItems(cart, selectedItemIds);

                if (!cart.Any())
                {
                    return Json(new { success = false, message = "Không có sản phẩm nào được chọn để thanh toán" });
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
                            if (item.SoLuong > spct.SoLuongKhaDung)
                            {
                                return Json(new
                                {
                                    success = false,
                                    message = $"Sản phẩm {spct.TenSanPham} chỉ còn {spct.SoLuongKhaDung} sản phẩm khả dụng."
                                });
                            }
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
                Guid? khachHangId = ResolveCurrentCustomerId();

                // Xử lý phiếu giảm giá nếu có
                Guid? phieuGiamGiaId = null;
                var maGiamGia = checkoutData.MaGiamGia?.Trim();
                if (!string.IsNullOrWhiteSpace(maGiamGia))
                {
                    var phieuResult = await KiemTraPhieuGiamGiaHopLeAsync(maGiamGia);
                    if (phieuResult == null || !phieuResult.Success || !phieuResult.IdPhieuGiamGia.HasValue)
                    {
                        return Json(new { success = false, message = "Mã giảm giá không hợp lệ hoặc đã hết hiệu lực." });
                    }

                    phieuGiamGiaId = phieuResult.IdPhieuGiamGia.Value;
                    checkoutData.MaGiamGia = maGiamGia;
                }
                else
                {
                    checkoutData.MaGiamGia = null;
                }

                if (!phieuGiamGiaId.HasValue && (checkoutData.TienGiam ?? 0) > 0)
                {
                    return Json(new { success = false, message = "Giảm giá không hợp lệ. Vui lòng chọn lại mã giảm giá." });
                }

                Console.WriteLine($"KhachHangId from claims: {khachHangId}");
                Console.WriteLine($"PhieuGiamGiaId: {phieuGiamGiaId}");
                Console.WriteLine($"MaGiamGia: {checkoutData.MaGiamGia}");

                // Tính phí vận chuyển động dựa trên API Shipping nếu có province/district
                try
                {
                    var orderValue = chiTietHoaDons.Sum(x => x.thanhTien);
                    if (!string.IsNullOrWhiteSpace(checkoutData.Province)
                        || checkoutData.ToDistrictId.GetValueOrDefault() > 0
                        || !string.IsNullOrWhiteSpace(checkoutData.ToWardCode))
                    {
                        var calcRequest = new CalculateShippingRequest
                        {
                            Province = checkoutData.Province ?? string.Empty,
                            District = checkoutData.District ?? string.Empty,
                            OrderValue = orderValue,
                            ToDistrictId = checkoutData.ToDistrictId,
                            ToWardCode = checkoutData.ToWardCode,
                            Weight = checkoutData.Weight,
                            CustomerId = khachHangId
                        };
                        var shippingResp = await _httpClient.PostAsJsonAsync("shipping/calculate", calcRequest);
                        if (shippingResp.IsSuccessStatusCode)
                        {
                            var shippingInfo = await shippingResp.Content.ReadFromJsonAsync<ShippingInfoDto>();
                            if (shippingInfo != null)
                            {
                                checkoutData.PhiVanChuyen = shippingInfo.FinalFee;
                                checkoutData.PhiVanChuyenDaGiam = true;
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
                            UsePoint = checkoutData.UsePoint,
                            RequestedUsedPoints = checkoutData.RequestedUsedPoints,
                            PhiVanChuyen = checkoutData.PhiVanChuyen,
                            PhiVanChuyenGoc = checkoutData.PhiVanChuyenGoc,
                            SoTienGiamPhiVanChuyen = checkoutData.SoTienGiamPhiVanChuyen,
                            ShippingDiscountMessage = checkoutData.ShippingDiscountMessage,
                            PhiVanChuyenDaGiam = checkoutData.PhiVanChuyenDaGiam,
                            TenNguoiNhan = checkoutData.TenNguoiNhan,
                            SoDienThoaiNguoiNhan = checkoutData.SoDienThoaiNguoiNhan,
                            DiaChiGiaoHang = checkoutData.DiaChiGiaoHang,
                            GhiChu = checkoutData.GhiChu,
                            PhieuGiamGiaId = phieuGiamGiaId,
                            Province = checkoutData.Province,
                            District = checkoutData.District,
                            MaGiamGia = checkoutData.MaGiamGia,
                            SelectedCartItemIds = selectedItemIds
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

                        return Json(new
                        {
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
                    usePoint = checkoutData.UsePoint,
                    requestedUsedPoints = checkoutData.RequestedUsedPoints,
                    phiVanChuyen = checkoutData.PhiVanChuyen, // Dùng phí đã tính từ API nếu có
                    phiVanChuyenDaGiam = checkoutData.PhiVanChuyenDaGiam,
                    phiVanChuyenGoc = checkoutData.PhiVanChuyenGoc,
                    soTienGiamPhiVanChuyen = checkoutData.SoTienGiamPhiVanChuyen,
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
                    decimal finalOrderTotal = checkoutData.TongTien;
                    decimal soTienGiamTuDiem = 0;
                    decimal tyLeQuyDoiDiem = 0;
                    int diemDaDung = checkoutData.RequestedUsedPoints ?? 0;
                    decimal phiVanChuyenGoc = checkoutData.PhiVanChuyenGoc ?? checkoutData.PhiVanChuyen;
                    decimal soTienGiamPhiVanChuyen = checkoutData.SoTienGiamPhiVanChuyen ?? Math.Max(phiVanChuyenGoc - checkoutData.PhiVanChuyen, 0);
                    string shippingDiscountMessage = checkoutData.ShippingDiscountMessage ?? string.Empty;

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
                            finalOrderTotal = hoaDonResponse?.TongTien ?? finalOrderTotal;
                            soTienGiamTuDiem = hoaDonResponse?.SoTienGiamTuDiem ?? 0;
                            tyLeQuyDoiDiem = hoaDonResponse?.TyLeQuyDoiDiem ?? 0;
                            diemDaDung = hoaDonResponse?.DiemDaDung ?? diemDaDung;
                        }

                        if (responseData.TryGetProperty("soTienGiamTuDiem", out var soTienGiamTuDiemElement))
                        {
                            soTienGiamTuDiem = soTienGiamTuDiemElement.GetDecimal();
                        }
                        if (responseData.TryGetProperty("tyLeQuyDoiDiem", out var tyLeQuyDoiDiemElement))
                        {
                            tyLeQuyDoiDiem = tyLeQuyDoiDiemElement.GetDecimal();
                        }
                        if (responseData.TryGetProperty("diemDaDung", out var diemDaDungElement))
                        {
                            diemDaDung = diemDaDungElement.GetInt32();
                        }
                        if (responseData.TryGetProperty("tongTien", out var tongTienElement))
                        {
                            finalOrderTotal = tongTienElement.GetDecimal();
                        }
                        else if (responseData.TryGetProperty("TongTien", out var tongTienPascalElement))
                        {
                            finalOrderTotal = tongTienPascalElement.GetDecimal();
                        }
                        if (responseData.TryGetProperty("phiVanChuyen", out var phiVanChuyenElement))
                        {
                            checkoutData.PhiVanChuyen = phiVanChuyenElement.GetDecimal();
                        }
                        if (responseData.TryGetProperty("phiVanChuyenGoc", out var phiVanChuyenGocElement))
                        {
                            phiVanChuyenGoc = phiVanChuyenGocElement.GetDecimal();
                        }
                        if (responseData.TryGetProperty("soTienGiamPhiVanChuyen", out var soTienGiamPhiVanChuyenElement))
                        {
                            soTienGiamPhiVanChuyen = soTienGiamPhiVanChuyenElement.GetDecimal();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing response: {ex.Message}");
                        // Sử dụng fallback
                    }

                    // Chuẩn bị dữ liệu cho trang Success
                    ViewBag.OrderCode = maHoaDon;
                    ViewBag.OrderTotal = finalOrderTotal;
                    ViewBag.PaymentMethod = "Thanh toán khi nhận hàng";
                    ViewBag.OrderDate = DateTime.Now;
                    ViewBag.CustomerName = checkoutData.TenNguoiNhan;
                    ViewBag.CustomerPhone = checkoutData.SoDienThoaiNguoiNhan;
                    ViewBag.CustomerAddress = checkoutData.DiaChiGiaoHang;
                    ViewBag.ShippingFee = checkoutData.PhiVanChuyen;
                    ViewBag.ShippingOriginalFee = phiVanChuyenGoc;
                    ViewBag.ShippingDiscountAmount = soTienGiamPhiVanChuyen;
                    ViewBag.ShippingDiscountMessage = shippingDiscountMessage;

                    await RemovePurchasedItemsFromCurrentCartAsync(selectedItemIds, khachHangId);
                    HttpContext.Session.Remove("SelectedCartItemIds");

                    return Json(new
                    {
                        success = true,
                        redirect = true,
                        redirectUrl = Url.Action("Success", "Checkout", new
                        {
                            orderCode = maHoaDon,
                            total = finalOrderTotal,
                            paymentMethod = "Thanh toán khi nhận hàng",
                            customerName = checkoutData.TenNguoiNhan,
                            customerPhone = checkoutData.SoDienThoaiNguoiNhan,
                            customerAddress = checkoutData.DiaChiGiaoHang,
                            shippingFee = checkoutData.PhiVanChuyen,
                            shippingOriginalFee = phiVanChuyenGoc,
                            shippingDiscountAmount = soTienGiamPhiVanChuyen,
                            shippingDiscountMessage = shippingDiscountMessage,
                            usedPoints = diemDaDung,
                            pointDiscount = soTienGiamTuDiem,
                            pointRate = tyLeQuyDoiDiem
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
        public async Task<IActionResult> CalculateShipping([FromBody] CalculateShippingRequest shippingData)
        {
            try
            {
                if (shippingData == null)
                {
                    return BadRequest(new { error = "Thiếu dữ liệu tính phí vận chuyển" });
                }

                if (!shippingData.CustomerId.HasValue)
                {
                    shippingData.CustomerId = ResolveCurrentCustomerId();
                }

                if (!shippingData.Weight.HasValue || shippingData.Weight.Value <= 0)
                {
                    shippingData.Weight = 500;
                }

                var response = await _httpClient.PostAsJsonAsync("shipping/calculate", shippingData);
                var result = await response.Content.ReadAsStringAsync();
                return new ContentResult
                {
                    StatusCode = (int)response.StatusCode,
                    Content = result,
                    ContentType = "application/json"
                };
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
                var result = await KiemTraPhieuGiamGiaHopLeAsync(code);
                if (result != null)
                {
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
        public async Task<IActionResult> ClearCart()
        {
            var selectedItemIds = GetSelectedCartItemIds();
            await RemovePurchasedItemsFromCurrentCartAsync(selectedItemIds);
            HttpContext.Session.Remove("SelectedCartItemIds");
            return Json(new { success = true });
        }

        // GET: Trang thành công
        public IActionResult Success(string orderCode, decimal? total = null, string paymentMethod = null,
            string customerName = null, string customerPhone = null, string customerAddress = null,
            decimal? shippingFee = null, decimal? shippingOriginalFee = null, decimal? shippingDiscountAmount = null, string? shippingDiscountMessage = null,
            int? usedPoints = null, decimal? pointDiscount = null, decimal? pointRate = null)
        {
            ViewBag.OrderCode = orderCode;
            ViewBag.OrderTotal = total ?? 0;
            ViewBag.PaymentMethod = paymentMethod ?? "Thanh toán khi nhận hàng";
            ViewBag.OrderDate = DateTime.Now;
            ViewBag.CustomerName = customerName;
            ViewBag.CustomerPhone = customerPhone;
            ViewBag.CustomerAddress = customerAddress;
            ViewBag.ShippingFee = shippingFee ?? 0;
            ViewBag.ShippingOriginalFee = shippingOriginalFee ?? (shippingFee ?? 0);
            ViewBag.ShippingDiscountAmount = shippingDiscountAmount ?? Math.Max((shippingOriginalFee ?? shippingFee ?? 0) - (shippingFee ?? 0), 0);
            ViewBag.ShippingDiscountMessage = shippingDiscountMessage ?? string.Empty;
            ViewBag.UsedPoints = usedPoints ?? 0;
            ViewBag.PointDiscount = pointDiscount ?? 0;
            ViewBag.PointRate = pointRate ?? 0;
            return View();
        }

        // GET: Test endpoint để kiểm tra
        [HttpGet]
        public IActionResult TestOrderCode()
        {
            var testOrderCode = $"HD_{DateTime.Now:yyyyMMddHHmmss}";
            return Json(new
            {
                success = true,
                message = $"Đặt hàng thành công! Mã đơn hàng của bạn là: {testOrderCode}",
                orderCode = testOrderCode
            });
        }

        [HttpGet]
        public IActionResult TestSession()
        {
            var pendingOrder = HttpContext.Session.GetString("PendingOrder");
            return Json(new
            {
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
        public async Task<IActionResult> GetPointInfo()
        {
            try
            {
                var customerId = ResolveCurrentCustomerId();
                if (!customerId.HasValue)
                {
                    return Json(new { success = true, isLoggedIn = false, availablePoints = 0, moneyPerPoint = 0m });
                }

                var customerResponse = await _httpClient.GetAsync($"KhachHang/{customerId.Value}");
                var configResponse = await _httpClient.GetAsync("CauHinhBanHang");
                if (!customerResponse.IsSuccessStatusCode || !configResponse.IsSuccessStatusCode)
                {
                    return Json(new { success = false, message = "Không thể lấy thông tin điểm khách hàng" });
                }

                using var customerDoc = JsonDocument.Parse(await customerResponse.Content.ReadAsStringAsync());
                using var configDoc = JsonDocument.Parse(await configResponse.Content.ReadAsStringAsync());

                var availablePoints = 0;
                if (customerDoc.RootElement.TryGetProperty("soDiemHienTai", out var soDiemCamel))
                {
                    availablePoints = soDiemCamel.GetInt32();
                }
                else if (customerDoc.RootElement.TryGetProperty("SoDiemHienTai", out var soDiemPascal))
                {
                    availablePoints = soDiemPascal.GetInt32();
                }

                decimal? moneyPerPoint = null;
                int maxPointsPerOrder = 0;
                if (configDoc.RootElement.TryGetProperty("config", out var config))
                {
                    if (config.TryGetProperty("soTienGiamTrenMotDiem", out var giamCamel))
                    {
                        moneyPerPoint = giamCamel.GetDecimal();
                    }
                    else if (config.TryGetProperty("SoTienGiamTrenMotDiem", out var giamPascal))
                    {
                        moneyPerPoint = giamPascal.GetDecimal();
                    }

                    if (config.TryGetProperty("diemToiDaSuDungMoiDon", out var maxCamel))
                    {
                        maxPointsPerOrder = maxCamel.GetInt32();
                    }
                    else if (config.TryGetProperty("DiemToiDaSuDungMoiDon", out var maxPascal))
                    {
                        maxPointsPerOrder = maxPascal.GetInt32();
                    }
                }

                if (!moneyPerPoint.HasValue || moneyPerPoint.Value <= 0)
                {
                    return Json(new { success = false, message = "Cấu hình quy đổi điểm chưa hợp lệ" });
                }

                return Json(new
                {
                    success = true,
                    isLoggedIn = true,
                    availablePoints = Math.Max(availablePoints, 0),
                    moneyPerPoint = moneyPerPoint.Value,
                    maxPointsPerOrder = Math.Max(maxPointsPerOrder, 0)
                });
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
                var selectedItemIds = GetSelectedCartItemIds();
                var cart = await GetCurrentCartAsync();
                await EnrichCartItemDetailsAsync(cart);
                cart = FilterCartBySelectedItems(cart, selectedItemIds);
                return Json(new { success = true, chiTietGioHangs = cart });
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
                return Json(new
                {
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
        public async Task<IActionResult> GetCustomerVouchers(decimal tongTien, string? soDienThoaiNguoiNhan = null, string? emailNguoiNhan = null)
        {
            try
            {
                var customerId = ResolveCurrentCustomerId();
                var query = $"KhachHangPhieuGiam/phieu-giam-gia-cong-khai?tongTien={tongTien}";
                if (customerId.HasValue)
                {
                    query += $"&customerId={customerId.Value}";
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(soDienThoaiNguoiNhan))
                    {
                        query += $"&soDienThoai={Uri.EscapeDataString(soDienThoaiNguoiNhan.Trim())}";
                    }

                    if (!string.IsNullOrWhiteSpace(emailNguoiNhan))
                    {
                        query += $"&email={Uri.EscapeDataString(emailNguoiNhan.Trim())}";
                    }
                }

                var response = await _httpClient.GetAsync(query);

                if (response.IsSuccessStatusCode)
                {
                    var vouchers = await response.Content.ReadFromJsonAsync<List<object>>();
                    return Json(vouchers);
                }

                return Json(new List<object>());
            }
            catch
            {
                return Json(new List<object>());
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
                    var vouchers = await response.Content.ReadFromJsonAsync<List<object>>();
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
                        if (checkoutInfo == null)
                        {
                            TempData["ErrorMessage"] = "Không đọc được dữ liệu đơn hàng tạm. Vui lòng thử lại.";
                            return RedirectToAction("Index", "GioHang");
                        }

                        // Lấy chi tiết hóa đơn từ nguồn giỏ hàng hiện tại
                        var cart = await GetCurrentCartAsync(checkoutInfo.KhachHangId);
                        var selectedItemIds = checkoutInfo.SelectedCartItemIds ?? GetSelectedCartItemIds();
                        cart = FilterCartBySelectedItems(cart, selectedItemIds);
                        if (!cart.Any())
                        {
                            TempData["ErrorMessage"] = "Không tìm thấy sản phẩm đã chọn để tạo đơn hàng.";
                            return RedirectToAction("Index", "GioHang");
                        }
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
                            usePoint = checkoutInfo.UsePoint,
                            xacNhanNgaySauThanhToan = true,
                            requestedUsedPoints = checkoutInfo.RequestedUsedPoints,
                            phiVanChuyen = checkoutInfo.PhiVanChuyen,
                            phiVanChuyenDaGiam = checkoutInfo.PhiVanChuyenDaGiam,
                            phiVanChuyenGoc = checkoutInfo.PhiVanChuyenGoc,
                            soTienGiamPhiVanChuyen = checkoutInfo.SoTienGiamPhiVanChuyen,
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
                                var root = doc.RootElement;
                                var maHoaDon = root.GetProperty("maHoaDon").GetString();
                                var diemDaDung = root.TryGetProperty("diemDaDung", out var diemDaDungEl)
                                    ? diemDaDungEl.GetInt32()
                                    : (checkoutInfo.RequestedUsedPoints ?? 0);
                                var finalOrderTotal = root.TryGetProperty("tongTien", out var tongTienEl)
                                    ? tongTienEl.GetDecimal()
                                    : (root.TryGetProperty("TongTien", out var tongTienPascalEl)
                                        ? tongTienPascalEl.GetDecimal()
                                        : checkoutInfo.TongTien);
                                var soTienGiamTuDiem = root.TryGetProperty("soTienGiamTuDiem", out var soTienGiamTuDiemEl)
                                    ? soTienGiamTuDiemEl.GetDecimal()
                                    : 0m;
                                var tyLeQuyDoiDiem = root.TryGetProperty("tyLeQuyDoiDiem", out var tyLeQuyDoiDiemEl)
                                    ? tyLeQuyDoiDiemEl.GetDecimal()
                                    : 0m;
                                var shippingFee = root.TryGetProperty("phiVanChuyen", out var shippingFeeEl)
                                    ? shippingFeeEl.GetDecimal()
                                    : checkoutInfo.PhiVanChuyen;
                                var shippingOriginalFee = root.TryGetProperty("phiVanChuyenGoc", out var shippingOriginalFeeEl)
                                    ? shippingOriginalFeeEl.GetDecimal()
                                    : (checkoutInfo.PhiVanChuyenGoc ?? shippingFee);
                                var shippingDiscountAmount = root.TryGetProperty("soTienGiamPhiVanChuyen", out var shippingDiscountAmountEl)
                                    ? shippingDiscountAmountEl.GetDecimal()
                                    : (checkoutInfo.SoTienGiamPhiVanChuyen ?? Math.Max(shippingOriginalFee - shippingFee, 0));

                                // Chuẩn bị dữ liệu cho trang Success
                                ViewBag.OrderCode = maHoaDon;
                                ViewBag.OrderTotal = finalOrderTotal;
                                ViewBag.PaymentMethod = "Thanh toán VNPay";
                                ViewBag.OrderDate = DateTime.Now;
                                ViewBag.CustomerName = checkoutInfo.TenNguoiNhan;
                                ViewBag.CustomerPhone = checkoutInfo.SoDienThoaiNguoiNhan;
                                ViewBag.CustomerAddress = checkoutInfo.DiaChiGiaoHang;
                                ViewBag.ShippingFee = shippingFee;
                                ViewBag.ShippingOriginalFee = shippingOriginalFee;
                                ViewBag.ShippingDiscountAmount = shippingDiscountAmount;
                                ViewBag.ShippingDiscountMessage = checkoutInfo.ShippingDiscountMessage ?? string.Empty;
                                ViewBag.UsedPoints = diemDaDung;
                                ViewBag.PointDiscount = soTienGiamTuDiem;
                                ViewBag.PointRate = tyLeQuyDoiDiem;

                                // Xóa giỏ hàng và thông tin đơn hàng tạm
                                await RemovePurchasedItemsFromCurrentCartAsync(selectedItemIds, checkoutInfo.KhachHangId);
                                HttpContext.Session.Remove("SelectedCartItemIds");
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
        public bool UsePoint { get; set; }
        public int? RequestedUsedPoints { get; set; }
        public string GhiChu { get; set; }
        public string MaGiamGia { get; set; }
        public decimal PhiVanChuyen { get; set; } = 50000;
        public decimal? PhiVanChuyenGoc { get; set; }
        public decimal? SoTienGiamPhiVanChuyen { get; set; }
        public string? ShippingDiscountMessage { get; set; }
        public bool PhiVanChuyenDaGiam { get; set; } = true;
        public int? ToDistrictId { get; set; }
        public string? ToWardCode { get; set; }
        public int? Weight { get; set; } = 500;
        public List<Guid>? SelectedCartItemIds { get; set; }
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
