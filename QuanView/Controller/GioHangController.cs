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

        private Guid? ResolveCurrentCustomerId()
        {
            var customerIdClaim = User.FindFirst("custom:id_khachhang");
            if (customerIdClaim != null && Guid.TryParse(customerIdClaim.Value, out var customerId))
            {
                return customerId;
            }

            return null;
        }

        private List<QuanApi.Data.ChiTietGioHang> GetSessionCart()
        {
            return HttpContext.Session.GetObjectFromJson<List<QuanApi.Data.ChiTietGioHang>>("Cart") ?? new List<QuanApi.Data.ChiTietGioHang>();
        }

        private void SaveSessionCart(List<QuanApi.Data.ChiTietGioHang> cart)
        {
            HttpContext.Session.SetObjectAsJson("Cart", cart ?? new List<QuanApi.Data.ChiTietGioHang>());
        }

        private async Task<QuanApi.Data.GioHang> GetDatabaseCartAsync(Guid customerId)
        {
            var response = await _httpClient.GetAsync($"GioHangs/user/{customerId}");
            if (!response.IsSuccessStatusCode)
            {
                return new QuanApi.Data.GioHang { IDKhachHang = customerId, ChiTietGioHangs = new List<QuanApi.Data.ChiTietGioHang>() };
            }

            var gioHang = await response.Content.ReadFromJsonAsync<QuanApi.Data.GioHang>();
            return gioHang ?? new QuanApi.Data.GioHang { IDKhachHang = customerId, ChiTietGioHangs = new List<QuanApi.Data.ChiTietGioHang>() };
        }

        private async Task MergeSessionCartToDbAsync(Guid customerId)
        {
            var cartSession = GetSessionCart();
            if (!cartSession.Any())
            {
                return;
            }

            var dbCart = await GetDatabaseCartAsync(customerId);
            var existingProductIds = dbCart.ChiTietGioHangs?
                .Select(x => x.IDSanPhamChiTiet)
                .ToHashSet() ?? new HashSet<Guid>();

            foreach (var item in cartSession)
            {
                // Rule: ưu tiên số lượng đã có trong DB nếu sản phẩm đã tồn tại.
                if (existingProductIds.Contains(item.IDSanPhamChiTiet))
                {
                    continue;
                }

                var addResponse = await _httpClient.PostAsync($"GioHangs/add?iduser={customerId}&idsp={item.IDSanPhamChiTiet}&soluong={item.SoLuong}", null);
                if (addResponse.IsSuccessStatusCode)
                {
                    existingProductIds.Add(item.IDSanPhamChiTiet);
                }
            }

            HttpContext.Session.Remove("Cart");
        }

        // GET: /GioHang/Index
        public async Task<IActionResult> Index(Guid? iduser)
        {
            var customerId = ResolveCurrentCustomerId();
            if (!customerId.HasValue)
            {
                var cart = GetSessionCart();
                foreach (var item in cart)
                {
                    var responseSpct = await _httpClient.GetAsync($"SanPhamChiTiets/{item.IDSanPhamChiTiet}");
                    if (responseSpct.IsSuccessStatusCode)
                    {
                        var spct = await responseSpct.Content.ReadFromJsonAsync<SanPhamChiTietDto>();
                        if (spct != null)
                        {
                            var soLuongKhaDung = spct.SoLuongKhaDung;
                            item.GiaBan = spct.price;
                            item.SanPhamChiTiet = new SanPhamChiTiet
                            {
                                SanPham = new SanPham
                                {
                                    TenSanPham = spct.TenSanPham
                                },
                                GiaBan = spct.GiaBan,
                                SoLuong = soLuongKhaDung,
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

                return View(new QuanApi.Data.GioHang { ChiTietGioHangs = cart });
            }

            await MergeSessionCartToDbAsync(customerId.Value);
            var gioHangDb = await GetDatabaseCartAsync(customerId.Value);

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

                        item.SanPhamChiTiet.SoLuong = spct.SoLuongKhaDung;
                        item.SanPhamChiTiet.GiaBan = spct.GiaBan;
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
                if (spct == null || spct.SoLuongKhaDung < soluong)
                {
                    return Json(new { success = false, message = "Số lượng vượt quá tồn kho!" });
                }

                var customerId = ResolveCurrentCustomerId();
                if (customerId.HasValue)
                {
                    await MergeSessionCartToDbAsync(customerId.Value);
                    var response = await _httpClient.PostAsync($"GioHangs/add?iduser={customerId.Value}&idsp={idsp}&soluong={soluong}", null);
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
                    var cart = GetSessionCart();

                    var existingItem = cart.FirstOrDefault(x => x.IDSanPhamChiTiet == idsp);
                    if (existingItem != null)
                    {
                        if (existingItem.SoLuong + soluong > spct.SoLuongKhaDung)
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

                    SaveSessionCart(cart);
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

                var customerId = ResolveCurrentCustomerId();
                Guid idsp;
                if (customerId.HasValue)
                {
                    var cartResponse = await _httpClient.GetAsync($"GioHangs/user/{customerId.Value}");
                    if (!cartResponse.IsSuccessStatusCode)
                    {
                        return RedirectToAction(nameof(Index));
                    }
                    var gioHang = await cartResponse.Content.ReadFromJsonAsync<QuanApi.Data.GioHang>();
                    var ghct = gioHang?.ChiTietGioHangs?.FirstOrDefault(c => c.IDChiTietGioHang == idghct);
                    idsp = ghct?.IDSanPhamChiTiet ?? Guid.Empty;
                }
                else
                {
                    var cart = GetSessionCart();
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
                    return RedirectToAction(nameof(Index));
                }
                var spct = await responseSpct.Content.ReadFromJsonAsync<SanPhamChiTietDto>();
                if (spct == null || spct.SoLuongKhaDung < soluong)
                {
                    TempData["ErrorMessage"] = "Số lượng vượt quá tồn kho!";
                    return RedirectToAction(nameof(Index));
                }

                if (customerId.HasValue)
                {
                    // Người dùng đã đăng nhập - cập nhật database
                    var response = await _httpClient.PutAsync($"GioHangs/item/{idghct}?soluong={soluong}", null);
                    if (response.IsSuccessStatusCode)
                    {
                        return RedirectToAction(nameof(Index));
                    }
                }
                else
                {
                    // Khách hàng - cập nhật session
                    var cart = GetSessionCart();
                    var item = cart.FirstOrDefault(x => x.IDChiTietGioHang == idghct);
                    if (item != null)
                    {
                        item.SoLuong = soluong;
                        SaveSessionCart(cart);
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
                var customerId = ResolveCurrentCustomerId();
                if (customerId.HasValue)
                {
                    // Người dùng đã đăng nhập - xóa từ database
                    var response = await _httpClient.DeleteAsync($"GioHangs/item/{idgiohang}");
                    if (response.IsSuccessStatusCode)
                    {
                        return RedirectToAction(nameof(Index));
                    }
                }
                else
                {
                    // Khách hàng - xóa từ session
                    var cart = GetSessionCart();
                    var item = cart.FirstOrDefault(x => x.IDChiTietGioHang == idgiohang);
                    if (item != null)
                    {
                        cart.Remove(item);
                        SaveSessionCart(cart);
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
                var response = await _httpClient.GetAsync($"GioHangs/user/{customerId}");
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

		// GET: /GioHang/Count
		[HttpGet]
		public async Task<IActionResult> Count()
		{
			try
			{
				var customerId = ResolveCurrentCustomerId();

				if (customerId.HasValue)
				{
					var gioHang = await GetDatabaseCartAsync(customerId.Value);

					var totalCount = gioHang.ChiTietGioHangs?
						.Select(x => x.IDSanPhamChiTiet)
						.Distinct()
						.Count() ?? 0;

					return Json(new { count = totalCount });
				}

				var cart = GetSessionCart();

				var sessionCount = cart
					.Select(x => x.IDSanPhamChiTiet)
					.Distinct()
					.Count();

				return Json(new { count = sessionCount });
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error getting cart count: {ex.Message}");
				return Json(new { count = 0 });
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

        // POST: /GioHang/CheckoutSelected
        [HttpPost]
        public IActionResult CheckoutSelected(string? selectedItemIds)
        {
            var selectedIds = new List<Guid>();

            if (!string.IsNullOrWhiteSpace(selectedItemIds))
            {
                selectedIds = selectedItemIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(id => Guid.TryParse(id, out var parsedId) ? parsedId : Guid.Empty)
                    .Where(id => id != Guid.Empty)
                    .Distinct()
                    .ToList();
            }

            if (!selectedIds.Any())
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất 1 sản phẩm để thanh toán.";
                return RedirectToAction(nameof(Index));
            }

            HttpContext.Session.SetObjectAsJson("SelectedCartItemIds", selectedIds);
            return RedirectToAction("Index", "Checkout");
        }

        public IActionResult PaymenCallBack()
        {
            return View();
        }
    }
}
