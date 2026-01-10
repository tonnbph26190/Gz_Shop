using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using QuanApi.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace QuanView.Controllers
{
    public class GioHangController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GioHangController> _logger;

        public GioHangController(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<GioHangController> logger)
        {
            _httpClient = httpClientFactory.CreateClient("MyApi");
            _configuration = configuration;
            _logger = logger;
        }

        // GET: /GioHang/Index
        public async Task<IActionResult> Index(Guid? iduser)
        {
            if (iduser == null || iduser == Guid.Empty)
            {
                var cart = HttpContext.Session.GetObjectFromJson<List<QuanApi.Data.ChiTietGioHang>>("Cart") ?? new List<QuanApi.Data.ChiTietGioHang>();
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
                                SoLuong = spct.SoLuong,
                                KichCo = new KichCo { TenKichCo = spct.TenKichCo },
                                MauSac = new MauSac { TenMauSac = spct.TenMauSac },
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
                var gioHang = new QuanApi.Data.GioHang { ChiTietGioHangs = cart };
                return View(gioHang);
            }
            var response = await _httpClient.GetAsync($"GioHangs/getbyuser?iduser={iduser}");
            if (!response.IsSuccessStatusCode)
            {
                ViewData["ErrorMessage"] = "Không thể tải giỏ hàng.";
                return View(new QuanApi.Data.GioHang { ChiTietGioHangs = new List<QuanApi.Data.ChiTietGioHang>() });
            }
            var gioHangDb = await response.Content.ReadFromJsonAsync<QuanApi.Data.GioHang>();
            if (gioHangDb == null)
            {
                gioHangDb = new QuanApi.Data.GioHang { ChiTietGioHangs = new List<QuanApi.Data.ChiTietGioHang>() };
            }
            // Populate SoLuongTon for db cart
            foreach (var item in gioHangDb.ChiTietGioHangs)
            {
                var responseSpct = await _httpClient.GetAsync($"SanPhamChiTiets/{item.IDSanPhamChiTiet}");
                if (responseSpct.IsSuccessStatusCode)
                {
                    var spct = await responseSpct.Content.ReadFromJsonAsync<SanPhamChiTietDto>();
                    if (spct != null)
                    {
                        item.GiaBan = spct.price;
                        if (item.SanPhamChiTiet == null)
                        {
                            item.SanPhamChiTiet = new SanPhamChiTiet();
                        }
                        item.SanPhamChiTiet.SoLuong = spct.SoLuong;
                        item.SanPhamChiTiet.SanPham = new SanPham { TenSanPham = spct.TenSanPham };
                        item.SanPhamChiTiet.KichCo = new KichCo { TenKichCo = spct.TenKichCo };
                        item.SanPhamChiTiet.MauSac = new MauSac { TenMauSac = spct.TenMauSac };
                        item.SanPhamChiTiet.AnhSanPhams = new List<AnhSanPham>
                        {
                            new AnhSanPham
                            {
                                UrlAnh = spct.AnhDaiDien ?? "/img/default-product.jpg",
                                LaAnhChinh = true
                            }
                        };
                    }
                }
            }
            var cartSession = HttpContext.Session.GetObjectFromJson<List<QuanApi.Data.ChiTietGioHang>>("Cart");
            if (cartSession != null && cartSession.Any())
            {
                foreach (var item in cartSession)
                {
                    await _httpClient.PostAsync($"GioHangs/add?iduser={iduser}&idsp={item.IDSanPhamChiTiet}&soluong={item.SoLuong}", null);
                }
                HttpContext.Session.Remove("Cart");
                var reload = await _httpClient.GetAsync($"GioHangs/getbyuser?iduser={iduser}");
                if (reload.IsSuccessStatusCode)
                    gioHangDb = await reload.Content.ReadFromJsonAsync<QuanApi.Data.GioHang>() ?? gioHangDb;
            }
            return View(gioHangDb);
        }

        // POST: /GioHang/Add
        [HttpPost]
        public async Task<IActionResult> Add(Guid? iduser, Guid idsp, int soluong)
        {
            try
            {
                var responseSpct = await _httpClient.GetAsync($"SanPhamChiTiets/{idsp}");
                if (!responseSpct.IsSuccessStatusCode)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm!" });
                }
                var spct = await responseSpct.Content.ReadFromJsonAsync<SanPhamChiTietDto>();
                if (spct == null || spct.SoLuong < soluong)
                {
                    return Json(new { success = false, message = "Số lượng vượt quá tồn kho!" });
                }

                if (iduser.HasValue && iduser != Guid.Empty)
                {
                    var response = await _httpClient.PostAsync($"GioHangs/add?iduser={iduser}&idsp={idsp}&soluong={soluong}", null);
                    if (response.IsSuccessStatusCode)
                    {
                        return Json(new { success = true, message = "Thêm vào giỏ hàng thành công!" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Thêm vào giỏ hàng thất bại!" });
                    }
                }
                else
                {
                    // Khách hàng - lưu vào session
                    var cart = HttpContext.Session.GetObjectFromJson<List<QuanApi.Data.ChiTietGioHang>>("Cart") ?? new List<QuanApi.Data.ChiTietGioHang>();

                    var existingItem = cart.FirstOrDefault(x => x.IDSanPhamChiTiet == idsp);
                    if (existingItem != null)
                    {
                        if (existingItem.SoLuong + soluong > spct.SoLuong)
                        {
                            return Json(new { success = false, message = "Số lượng vượt quá tồn kho!" });
                        }
                        existingItem.SoLuong += soluong;
                    }
                    else
                    {
                        cart.Add(new QuanApi.Data.ChiTietGioHang
                        {
                            IDChiTietGioHang = Guid.NewGuid(),
                            IDSanPhamChiTiet = idsp,
                            SoLuong = soluong,
                            GiaBan = spct.price
                        });
                    }

                    HttpContext.Session.SetObjectAsJson("Cart", cart);
                    return Json(new { success = true, message = "Thêm vào giỏ hàng thành công!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding item to cart: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm vào giỏ hàng!" });
            }
        }

        // POST: /GioHang/Update
        [HttpPost]
        public async Task<IActionResult> Update(Guid idghct, int soluong, Guid? iduser)
        {
            try
            {
                if (soluong < 1)
                {
                    return await Delete(idghct, iduser);
                }

                Guid idsp;
                if (iduser.HasValue && iduser != Guid.Empty)
                {
                    // Lấy ID sản phẩm từ database
                    var ghctResponse = await _httpClient.GetAsync($"GioHangs/get-chi-tiet?idghct={idghct}");
                    if (!ghctResponse.IsSuccessStatusCode)
                    {
                        return RedirectToAction(nameof(Index), new { iduser = iduser });
                    }
                    var ghct = await ghctResponse.Content.ReadFromJsonAsync<QuanApi.Data.ChiTietGioHang>();
                    idsp = ghct?.IDSanPhamChiTiet ?? Guid.Empty;
                }
                else
                {
                    var cart = HttpContext.Session.GetObjectFromJson<List<QuanApi.Data.ChiTietGioHang>>("Cart") ?? new List<QuanApi.Data.ChiTietGioHang>();
                    var item = cart.FirstOrDefault(x => x.IDChiTietGioHang == idghct);
                    if (item == null)
                    {
                        return RedirectToAction(nameof(Index));
                    }
                    idsp = item.IDSanPhamChiTiet;
                }

                // Kiểm tra tồn kho
                var responseSpct = await _httpClient.GetAsync($"SanPhamChiTiets/{idsp}");
                if (!responseSpct.IsSuccessStatusCode)
                {
                    return RedirectToAction(nameof(Index), new { iduser = iduser });
                }
                var spct = await responseSpct.Content.ReadFromJsonAsync<SanPhamChiTietDto>();
                if (spct == null || spct.SoLuong < soluong)
                {
                    TempData["ErrorMessage"] = "Số lượng vượt quá tồn kho!";
                    return RedirectToAction(nameof(Index), new { iduser = iduser });
                }

                if (iduser.HasValue && iduser != Guid.Empty)
                {
                    // Người dùng đã đăng nhập - cập nhật database
                    var response = await _httpClient.PutAsync($"GioHangs/update?idghct={idghct}&soluong={soluong}", null);
                    if (response.IsSuccessStatusCode)
                    {
                        return RedirectToAction(nameof(Index), new { iduser = iduser });
                    }
                }
                else
                {
                    // Khách hàng - cập nhật session
                    var cart = HttpContext.Session.GetObjectFromJson<List<QuanApi.Data.ChiTietGioHang>>("Cart") ?? new List<QuanApi.Data.ChiTietGioHang>();
                    var item = cart.FirstOrDefault(x => x.IDChiTietGioHang == idghct);
                    if (item != null)
                    {
                        item.SoLuong = soluong;
                        HttpContext.Session.SetObjectAsJson("Cart", cart);
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating cart: {ex.Message}");
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /GioHang/Delete
        [HttpPost]
        public async Task<IActionResult> Delete(Guid idgiohang, Guid? iduser)
        {
            try
            {
                if (iduser.HasValue && iduser != Guid.Empty)
                {
                    // Người dùng đã đăng nhập - xóa từ database
                    var response = await _httpClient.DeleteAsync($"GioHangs/xoa?idgiohang={idgiohang}");
                    if (response.IsSuccessStatusCode)
                    {
                        return RedirectToAction(nameof(Index), new { iduser = iduser });
                    }
                }
                else
                {
                    // Khách hàng - xóa từ session
                    var cart = HttpContext.Session.GetObjectFromJson<List<QuanApi.Data.ChiTietGioHang>>("Cart") ?? new List<QuanApi.Data.ChiTietGioHang>();
                    var item = cart.FirstOrDefault(x => x.IDChiTietGioHang == idgiohang);
                    if (item != null)
                    {
                        cart.Remove(item);
                        HttpContext.Session.SetObjectAsJson("Cart", cart);
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting cart item: {ex.Message}");
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: GioHang/GetGioHang
        [HttpGet]
        public async Task<IActionResult> GetGioHang()
        {
            try
            {
                var customerIdClaim = User.FindFirst("custom:id_khachhang");
                if (customerIdClaim == null || !Guid.TryParse(customerIdClaim.Value, out var customerId))
                {
                    // Khách hàng - trả về giỏ hàng session
                    var cart = HttpContext.Session.GetObjectFromJson<List<QuanApi.Data.ChiTietGioHang>>("Cart") ?? new List<QuanApi.Data.ChiTietGioHang>();
                    return Json(cart);
                }

                // Người dùng đã đăng nhập - trả về giỏ hàng từ database
                var response = await _httpClient.GetAsync($"GioHangs/getbyuser?iduser={customerId}");
                if (response.IsSuccessStatusCode)
                {
                    var gioHang = await response.Content.ReadFromJsonAsync<QuanApi.Data.GioHang>();
                    return Json(gioHang?.ChiTietGioHangs ?? new List<QuanApi.Data.ChiTietGioHang>());
                }

                return Json(new List<QuanApi.Data.ChiTietGioHang>());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting cart: {ex.Message}");
                return Json(new List<QuanApi.Data.ChiTietGioHang>());
            }
        }

        // GET: GioHang/GetCustomerVouchers
        [HttpGet]
        public async Task<IActionResult> GetCustomerVouchers()
        {
            try
            {
                var customerIdClaim = User.FindFirst("custom:id_khachhang");
                Guid? customerId = null;

                if (customerIdClaim != null && Guid.TryParse(customerIdClaim.Value, out var parsedCustomerId))
                {
                    customerId = parsedCustomerId;
                    _logger.LogInformation($"Getting vouchers for logged-in customer: {customerId}");
                }
                else
                {
                    _logger.LogInformation("Getting public vouchers for guest user");
                }

                // Lấy phiếu giảm giá công khai cho tất cả người dùng
                var response = await _httpClient.GetAsync($"PhieuGiamGias/public-vouchers");

                if (response.IsSuccessStatusCode)
                {
                    var vouchers = await response.Content.ReadFromJsonAsync<List<object>>();
                    _logger.LogInformation($"Found {vouchers?.Count ?? 0} public vouchers");
                    return Json(vouchers ?? new List<object>());
                }
                else
                {
                    _logger.LogWarning($"API returned status: {response.StatusCode}");
                }

                return Json(new List<object>());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting customer vouchers: {ex.Message}");
                return Json(new List<object>());
            }
        }
        public IActionResult PaymenCallBack()
        {
            return View();
        }
    }
}
