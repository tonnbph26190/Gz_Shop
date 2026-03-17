using Microsoft.AspNetCore.Mvc;
using QuanApi.Controllers;
using QuanApi.Data;
using QuanApi.Dtos;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace QuanView.Controllers
{
    public class SanPhamNguoiDungController : Controller
    {
        private readonly HttpClient _http;

        public SanPhamNguoiDungController(IHttpClientFactory httpClientFactory)
        {
            _http = httpClientFactory.CreateClient("MyApi");
        }

        // GET: /SanPhamNguoiDung/Index
        public async Task<IActionResult> Index(string search, int? priceFrom, int? priceTo, string category, string size, string color, string sortOrder, int page = 1)
        {
            var pageSize = 9;
            var query = $"SanPhamNguoiDungs?pageNumber={page}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(search))
                query += $"&search={Uri.EscapeDataString(search)}";
            if (priceFrom.HasValue)
                query += $"&priceFrom={priceFrom.Value}";
            if (priceTo.HasValue)
                query += $"&priceTo={priceTo.Value}";
            if (!string.IsNullOrEmpty(category))
                query += $"&category={Uri.EscapeDataString(category)}";
            if (!string.IsNullOrEmpty(size))
                query += $"&size={Uri.EscapeDataString(size)}";
            if (!string.IsNullOrEmpty(color))
                query += $"&color={Uri.EscapeDataString(color)}";

            var response = await _http.GetAsync(query);
            if (!response.IsSuccessStatusCode)
            {
                ViewData["ErrorMessage"] = "Không thể tải danh sách sản phẩm.";
                return View("Error");
            }
            var list = await response.Content.ReadFromJsonAsync<List<SanPhamKhachHangViewModel>>();
            if (!string.IsNullOrEmpty(sortOrder) && list != null)
            {
                if (sortOrder == "asc")
                {
                    list = list.OrderBy(sp => sp.BienThes != null && sp.BienThes.Any() ? sp.BienThes.Min(b => b.GiaSauGiam) : decimal.MaxValue).ToList();
                }
                else if (sortOrder == "desc")
                {
                    list = list.OrderByDescending(sp => sp.BienThes != null && sp.BienThes.Any() ? sp.BienThes.Min(b => b.GiaSauGiam) : decimal.MinValue).ToList();
                }
            }
            // Fetch filter options
            var filterResponse = await _http.GetAsync("SanPhamNguoiDungs/filter-options");
            if (filterResponse.IsSuccessStatusCode)
            {
                var filterOptions = await filterResponse.Content.ReadFromJsonAsync<FilterOptionsDto>();
                ViewBag.Categories = filterOptions?.Categories ?? new List<string>();
                ViewBag.Sizes = filterOptions?.Sizes ?? new List<string>();
                ViewBag.Colors = filterOptions?.Colors ?? new List<string>();
            }
            else
            {
                ViewBag.Categories = new List<string>();
                ViewBag.Sizes = new List<string>();
                ViewBag.Colors = new List<string>();
            }

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = list?.Count ?? 0;
            return View(list);
        }

		// GET: /SanPhamNguoiDung/Detail/{id}
		// GET: /SanPhamNguoiDung/Detail/{id}
		public async Task<IActionResult> Detail(Guid id)
		{
			var bienTheRes = await _http.GetAsync($"SanPhamNguoiDungs/{id}");
			if (!bienTheRes.IsSuccessStatusCode)
			{
				ViewData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
				return View("Error");
			}
			var bienThe = await bienTheRes.Content.ReadFromJsonAsync<SanPhamChiTietDto>();
			if (bienThe == null)
			{
				ViewData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
				return View("Error");
			}

			// --- CALL NEW DETAIL ENDPOINT ---
			var detailRes = await _http.GetAsync($"SanPhamChiTiets/getDetailSp?idsanpham={bienThe.IdSanPham}");
			if (!detailRes.IsSuccessStatusCode)
			{
				ViewData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
				return View("Error");
			}
			var spDetail = await detailRes.Content.ReadFromJsonAsync<SanPhamDetailDto>();
			if (spDetail == null || !spDetail.BienThes.Any())
			{
				ViewData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
				return View("Error");
			}

			// Lấy ảnh hiển thị chính: ưu tiên ảnh đầu ở DanhSachAnh, fallback sang ảnh biến thể
			string urlAnh = spDetail.DanhSachAnh?.FirstOrDefault()
							?? spDetail.BienThes.SelectMany(b => new[] { b.AnhDaiDien }).FirstOrDefault(s => !string.IsNullOrEmpty(s))
							?? "/img/default-product.jpg";

			var firstBienThe = spDetail.BienThes.FirstOrDefault() ?? bienThe;

			var model = new SanPhamKhachHangViewModel
			{
				TenSanPham = spDetail.TenSanPham ?? bienThe.TenSanPham,
				DanhMuc = spDetail.TenDanhMuc ?? firstBienThe.TenDanhMuc ?? "",
				UrlAnh = urlAnh,
				DanhSachAnh = spDetail.DanhSachAnh ?? new List<string> { urlAnh },
				BienThes = spDetail.BienThes.Select(b => new BienTheSanPhamViewModel
				{
					IDSanPhamChiTiet = b.IdSanPhamChiTiet,
					Size = b.TenKichCo,
					Mau = b.TenMauSac,
					HoaTiet = b.TenHoaTiet,
					GiaGoc = b.GiaBan,
					GiaSauGiam = b.price,
					SoLuong = b.SoLuong
				}).ToList()
			};

			return View(model);
		}
	}
}
