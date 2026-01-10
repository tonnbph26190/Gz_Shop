using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Net.Http.Json;
using QuanApi.Dtos;
using QuanView.Services;
using QuanView.Models;

namespace QuanView.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminPolicy")]
    public class ClientBanHangTaiQuayController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IVnPayService _vnPayService;
        private readonly IConfiguration _configuration;
        public ClientBanHangTaiQuayController(IHttpClientFactory httpClientFactory, IVnPayService vnPayService, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient("MyApi");
            _vnPayService = vnPayService;
            _configuration = configuration;
        }
        public IActionResult Index()
        {
            return View();
        }

        // Lấy danh sách sản phẩm
        [HttpGet]
        [Route("Admin/ClientBanHangTaiQuay/danh-sach-san-pham")]
        public async Task<IActionResult> GetProducts()
        {
            var response = await _httpClient.GetAsync("BanHangTaiQuay/danh-sach-san-pham");
            var result = await response.Content.ReadAsStringAsync();
            return Content(result, "application/json");
        }

        // Lấy danh sách khách hàng
        [HttpGet]
        [Route("Admin/ClientBanHangTaiQuay/danh-sach-khach-hang")]
        public async Task<IActionResult> GetCustomers()
        {
            var response = await _httpClient.GetAsync("BanHangTaiQuay/danh-sach-khach-hang");
            var result = await response.Content.ReadAsStringAsync();
            return Content(result, "application/json");
        }

        // Tìm kiếm khách hàng
        [HttpGet]
        [Route("Admin/ClientBanHangTaiQuay/tim-kiem-khach-hang")]
        public async Task<IActionResult> SearchCustomer(string query)
        {
            var response = await _httpClient.GetAsync($"BanHangTaiQuay/tim-kiem-khach-hang?query={Uri.EscapeDataString(query)}");
            var result = await response.Content.ReadAsStringAsync();
            return Content(result, "application/json");
        }

        // Thêm khách hàng mới
        [HttpPost]
        [Route("Admin/ClientBanHangTaiQuay/them-khach-hang")]
        public async Task<IActionResult> AddCustomer([FromBody] TaoKhachHangDto dto)
        {
            var response = await _httpClient.PostAsJsonAsync("BanHangTaiQuay/them-khach-hang", dto);
            var result = await response.Content.ReadAsStringAsync();
            return Content(result, "application/json");
        }

        // Kiểm tra mã giảm giá
        [HttpGet]
        [Route("Admin/ClientBanHangTaiQuay/kiem-tra-ma-giam-gia")]
        public async Task<IActionResult> CheckDiscount(string code)
        {
            var response = await _httpClient.GetAsync($"BanHangTaiQuay/kiem-tra-ma-giam-gia?code={Uri.EscapeDataString(code)}");
            var result = await response.Content.ReadAsStringAsync();
            return Content(result, "application/json");
        }

        // Khởi tạo URL thanh toán VNPay cho POS (không ảnh hưởng online)
        [HttpPost]
        [Route("Admin/ClientBanHangTaiQuay/vnpay/init")]
        public IActionResult InitVnpay([FromBody] PaymentInformationModel model)
        {
            // ReturnUrl riêng cho POS
            var posReturnUrl = _configuration["PaymentCallBack:PosReturnUrl"] ?? Url.Action("VnPayPosCallback", "ClientBanHangTaiQuay", new { area = "Admin" }, Request.Scheme);
            var url = _vnPayService.CreatePaymentUrl(model, HttpContext, posReturnUrl);
            return Json(new { success = true, paymentUrl = url });
        }

        // VNPay callback cho POS: Sau khi thanh toán thành công, chuyển tới SuccessBHTQ
        [HttpGet]
        [Route("Admin/ClientBanHangTaiQuay/VnPayPosCallback")]
        public async Task<IActionResult> VnPayPosCallback()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);

            if (response.Success && response.VnPayResponseCode == "00")
            {
                // Ở POS: mã đơn có thể lấy từ session tạm hoặc query nếu đã tạo trước
                var orderIdStr = HttpContext.Session.GetString("POS_LastOrderId");
                if (!string.IsNullOrEmpty(orderIdStr) && Guid.TryParse(orderIdStr, out var orderId))
                {
                    var res = await _httpClient.GetAsync($"HoaDons/{orderId}");
                    if (res.IsSuccessStatusCode)
                    {
                        var json = await res.Content.ReadAsStringAsync();
                        TempData["OrderJson"] = json;
                        return View("~/Areas/Admin/Views/ClientBanHangTaiQuay/SuccessBHTQ.cshtml");
                    }
                }
                // Fallback: chỉ hiển thị thông báo thành công nếu thiếu orderId
                TempData["OrderJson"] = null;
                return View("~/Areas/Admin/Views/ClientBanHangTaiQuay/SuccessBHTQ.cshtml");
            }

            TempData["ErrorMessage"] = "Thanh toán VNPay thất bại hoặc bị hủy.";
            return RedirectToAction("Index");
        }

        // Tính phí vận chuyển
        [HttpPost]
        [Route("Admin/ClientBanHangTaiQuay/tinh-phi-van-chuyen")]
        public async Task<IActionResult> CalculateShippingFee([FromBody] object shippingData)
        {
            var response = await _httpClient.PostAsJsonAsync("shipping/calculate", shippingData);
            var result = await response.Content.ReadAsStringAsync();
            return Content(result, "application/json");
        }

        // Thanh toán hóa đơn
        [HttpPost]
        [Route("Admin/ClientBanHangTaiQuay/thanh-toan")]
        public async Task<IActionResult> PayInvoice([FromBody] object dto)
        {
            var response = await _httpClient.PostAsJsonAsync("BanHangTaiQuay/thanh-toan", dto);
            var result = await response.Content.ReadAsStringAsync();
            return Content(result, "application/json");
        }

        // Lấy danh sách phương thức thanh toán
        [HttpGet]
        [Route("Admin/ClientBanHangTaiQuay/danh-sach-phuong-thuc-thanh-toan")]
        public async Task<IActionResult> GetPaymentMethods()
        {
            try
            {
                var response = await _httpClient.GetAsync("BanHangTaiQuay/danh-sach-phuong-thuc-thanh-toan");
                if (response.IsSuccessStatusCode)
                {
                    var methods = await response.Content.ReadFromJsonAsync<object>();
                    return Ok(methods);
                }
                return StatusCode((int)response.StatusCode, "Lỗi khi lấy danh sách phương thức thanh toán");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("Admin/ClientBanHangTaiQuay/danh-sach-phieu-giam-gia-khach-hang")]
        public async Task<IActionResult> GetCustomerDiscountVouchers(Guid customerId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"BanHangTaiQuay/danh-sach-phieu-giam-gia-khach-hang?customerId={customerId}");
                if (response.IsSuccessStatusCode)
                {
                    var vouchers = await response.Content.ReadFromJsonAsync<object>();
                    return Ok(vouchers);
                }
                return StatusCode((int)response.StatusCode, "Lỗi khi lấy danh sách phiếu giảm giá");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi: {ex.Message}");
            }
        }

        // Lấy địa chỉ của khách hàng
        [HttpGet]
        [Route("Admin/ClientBanHangTaiQuay/dia-chi-khach-hang")]
        public async Task<IActionResult> GetCustomerAddress(Guid customerId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"BanHangTaiQuay/dia-chi-khach-hang?customerId={customerId}");
                if (response.IsSuccessStatusCode)
                {
                    var address = await response.Content.ReadFromJsonAsync<object>();
                    return Ok(address);
                }
                return StatusCode((int)response.StatusCode, "Lỗi khi lấy địa chỉ khách hàng");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi: {ex.Message}");
            }
        }

        // Tạo địa chỉ mới cho khách hàng
        [HttpPost]
        [Route("Admin/ClientBanHangTaiQuay/tao-dia-chi")]
        public async Task<IActionResult> TaoDiaChi([FromBody] TaoDiaChiDto dto)
        {
            try
            {
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

        // Lấy danh sách địa chỉ của khách hàng
        [HttpGet]
        [Route("Admin/ClientBanHangTaiQuay/danh-sach-dia-chi-khach-hang")]
        public async Task<IActionResult> GetCustomerAddresses(Guid customerId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"BanHangTaiQuay/danh-sach-dia-chi-khach-hang?customerId={customerId}");
                if (response.IsSuccessStatusCode)
                {
                    var addresses = await response.Content.ReadFromJsonAsync<object>();
                    return Ok(addresses);
                }
                return StatusCode((int)response.StatusCode, "Lỗi khi lấy danh sách địa chỉ khách hàng");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi: {ex.Message}");
            }
        }

        // Tạo giỏ hàng mới
        [HttpPost]
        [Route("Admin/ClientBanHangTaiQuay/tao-gio-hang")]
        public async Task<IActionResult> TaoGioHang([FromBody] TaoGioHangDto dto)
        {
            var response = await _httpClient.PostAsJsonAsync("BanHangTaiQuay/tao-gio-hang", dto);
            var result = await response.Content.ReadAsStringAsync();
            return Content(result, "application/json");
        }

        // Thêm sản phẩm vào giỏ hàng
        [HttpPost]
        [Route("Admin/ClientBanHangTaiQuay/them-vao-gio-hang")]
        public async Task<IActionResult> ThemVaoGioHang([FromBody] ThemVaoGioHangDto dto)
        {
            var response = await _httpClient.PostAsJsonAsync("BanHangTaiQuay/them-vao-gio-hang", dto);
            var result = await response.Content.ReadAsStringAsync();
            return Content(result, "application/json");
        }

        // Cập nhật số lượng trong giỏ hàng
        [HttpPost]
        [Route("Admin/ClientBanHangTaiQuay/cap-nhat-so-luong-gio-hang")]
        public async Task<IActionResult> CapNhatSoLuongGioHang([FromBody] CapNhatSoLuongGioHangDto dto)
        {
            var response = await _httpClient.PostAsJsonAsync("BanHangTaiQuay/cap-nhat-so-luong-gio-hang", dto);
            var result = await response.Content.ReadAsStringAsync();
            return Content(result, "application/json");
        }

        // Xóa sản phẩm khỏi giỏ hàng
        [HttpPost]
        [Route("Admin/ClientBanHangTaiQuay/xoa-khoi-gio-hang")]
        public async Task<IActionResult> XoaKhoiGioHang([FromBody] XoaKhoiGioHangDto dto)
        {
            var response = await _httpClient.PostAsJsonAsync("BanHangTaiQuay/xoa-khoi-gio-hang", dto);
            var result = await response.Content.ReadAsStringAsync();
            return Content(result, "application/json");
        }

        // Lấy thông tin giỏ hàng
        [HttpGet]
        [Route("Admin/ClientBanHangTaiQuay/gio-hang/{idGioHang}")]
        public async Task<IActionResult> GetGioHang(Guid idGioHang)
        {
            var response = await _httpClient.GetAsync($"BanHangTaiQuay/gio-hang/{idGioHang}");
            var result = await response.Content.ReadAsStringAsync();
            return Content(result, "application/json");
        }

        // Xóa giỏ hàng
        [HttpPost]
        [Route("Admin/ClientBanHangTaiQuay/xoa-gio-hang")]
        public async Task<IActionResult> XoaGioHang([FromBody] XoaGioHangDto dto)
        {
            var response = await _httpClient.PostAsJsonAsync("BanHangTaiQuay/xoa-gio-hang", dto);
            var result = await response.Content.ReadAsStringAsync();
            return Content(result, "application/json");
        }

        // Chuyển giỏ hàng thành hóa đơn
        [HttpPost]
        [Route("Admin/ClientBanHangTaiQuay/chuyen-gio-hang-thanh-hoa-don")]
        public async Task<IActionResult> ChuyenGioHangThanhHoaDon([FromBody] ChuyenGioHangThanhHoaDonDto dto)
        {
            var response = await _httpClient.PostAsJsonAsync("BanHangTaiQuay/chuyen-gio-hang-thanh-hoa-don", dto);
            var result = await response.Content.ReadAsStringAsync();
            return Content(result, "application/json");
        }

        // Lấy chi tiết hóa đơn theo ID (wrapper)
        [HttpGet]
        [Route("Admin/ClientBanHangTaiQuay/hoa-don/{id}")]
        public async Task<IActionResult> GetHoaDonById(Guid id)
        {
            var response = await _httpClient.GetAsync($"HoaDons/{id}");
            var result = await response.Content.ReadAsStringAsync();
            return Content(result, "application/json");
        }
    }
}