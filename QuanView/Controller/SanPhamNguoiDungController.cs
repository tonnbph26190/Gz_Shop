using Microsoft.AspNetCore.Mvc;
using QuanApi.Controllers;
using QuanApi.Data;
using QuanApi.Dtos;
using QuanView.ViewModels;
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
		public async Task<IActionResult> Index(string search, int? priceFrom, int? priceTo, string category, string size, string color, string sortOrder, string stockFilter, int page = 1)
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
			if (!string.IsNullOrEmpty(sortOrder))
				query += $"&sortOrder={sortOrder}";
			if (!string.IsNullOrEmpty(stockFilter))   // 👈 THÊM DÒNG NÀY
				query += $"&stockFilter={stockFilter}";
			var response = await _http.GetAsync(query);
			if (!response.IsSuccessStatusCode)
			{
				ViewData["ErrorMessage"] = "Không thể tải danh sách sản phẩm.";
				return View("Error");
			}

			var result = await response.Content.ReadFromJsonAsync<PagedResult<SanPhamKhachHangViewModel>>();

			var list = result?.Data ?? new List<SanPhamKhachHangViewModel>();
			var total = result?.Total ?? 0;

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
			ViewBag.Total = total;
			return View(list);
		}

		// GET: /SanPhamNguoiDung/Detail/{id}
		// GET: /SanPhamNguoiDung/Detail/{id}
		public async Task<IActionResult> Detail(Guid id)
		{
			var bienTheRes = await _http.GetAsync($"SanPhamNguoiDungs/{id}");
			if (!bienTheRes.IsSuccessStatusCode)
			{
				TempData["Warning"] = "Sản phẩm đã ngưng hoạt động hoặc không còn tồn tại.";
				return RedirectToAction("Index");
			}
			var bienThe = await bienTheRes.Content.ReadFromJsonAsync<SanPhamChiTietDto>();
			if (bienThe == null)
			{
				TempData["Warning"] = "Sản phẩm đã ngưng hoạt động hoặc không còn tồn tại.";
				return RedirectToAction("Index");
			}

			return await RenderDetailByProductId(bienThe.IdSanPham, bienThe.TenSanPham, bienThe.TenDanhMuc);
		}

		// GET: /SanPhamNguoiDung/DetailByProduct/{id}
		public async Task<IActionResult> DetailByProduct(Guid id)
		{
			return await RenderDetailByProductId(id, null, null);
		}

		private async Task<IActionResult> RenderDetailByProductId(Guid productId, string? fallbackProductName, string? fallbackCategory)
		{
			var detailRes = await _http.GetAsync($"SanPhamChiTiets/getDetailSp?idsanpham={productId}");
			if (!detailRes.IsSuccessStatusCode)
			{
				ViewData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
				TempData["Warning"] = "Sản phẩm đã ngưng hoạt động hoặc không còn tồn tại.";
				return RedirectToAction("Index");
			}

			var spDetail = await detailRes.Content.ReadFromJsonAsync<SanPhamDetailDto>();

			if (spDetail == null || spDetail.BienThes == null)
			{
				ViewData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
				TempData["Warning"] = "Sản phẩm đã ngưng hoạt động hoặc không còn tồn tại.";
				return RedirectToAction("Index");
			}

			spDetail.BienThes = spDetail.BienThes
				.Where(x => x.TrangThai == true)
				.ToList();

			if (!spDetail.BienThes.Any())
			{
				ViewData["ErrorMessage"] = "Sản phẩm đã ngưng hoạt động hoặc không còn biến thể.";
				TempData["Warning"] = "Sản phẩm đã ngưng hoạt động hoặc không còn tồn tại.";
				return RedirectToAction("Index");
			}

			var firstBienThe = spDetail.BienThes.First();

			var danhSachAnhBanDau = spDetail.BienThes
				.SelectMany(x => x.DanhSachAnh != null && x.DanhSachAnh.Any()
					? x.DanhSachAnh
					: new List<string> { x.AnhDaiDien ?? "" })
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Distinct()
				.ToList();

			if (!danhSachAnhBanDau.Any())
			{
				danhSachAnhBanDau.Add("/img/default-product.jpg");
			}

			string urlAnh = danhSachAnhBanDau.FirstOrDefault() ?? "/img/default-product.jpg";
			var model = new SanPhamKhachHangViewModel
			{
				TenSanPham = spDetail.TenSanPham ?? fallbackProductName ?? "Sản phẩm",
				DanhMuc = spDetail.TenDanhMuc ?? fallbackCategory ?? firstBienThe.TenDanhMuc ?? "",
				UrlAnh = urlAnh,
				DanhSachAnh = danhSachAnhBanDau,
				BienThes = spDetail.BienThes.Select(b => new BienTheSanPhamViewModel
				{
					IDSanPhamChiTiet = b.IdSanPhamChiTiet,
					Size = b.TenKichCo,
					Mau = b.TenMauSac,
					HoaTiet = b.TenHoaTiet,
					GiaGoc = b.GiaBan,
					GiaSauGiam = b.price,
					SoLuong = b.SoLuongKhaDung,
					AnhDaiDien = b.AnhDaiDien,
					DanhSachAnh = b.DanhSachAnh != null && b.DanhSachAnh.Any()
		? b.DanhSachAnh
		: new List<string> { b.AnhDaiDien ?? "/img/default-product.jpg" }
				}).ToList()
			};

			return View("Detail", model);
		}
	}
}
