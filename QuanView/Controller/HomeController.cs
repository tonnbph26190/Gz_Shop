using System.Diagnostics;
using System.Text.Json;
using BanQuanAu1.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Crypto;
using QuanApi.Dtos;
using QuanView.Models;

namespace QuanView.Controllers
{
    public class HomeController : Controller
    {
        private readonly BanQuanAu1DbContext _context;
        private readonly HttpClient _httpClient;

        public HomeController(BanQuanAu1DbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClient = httpClientFactory.CreateClient("MyApi");
        }

		public async Task<IActionResult> Index()
		{
			var banners = _context.Banners.ToList();
			var featuredProducts = new List<SanPhamKhachHangViewModel>();

			try
			{
				// 1. Fetch the raw string
				var jsonString = await _httpClient.GetStringAsync("SanPhamNguoiDungs?pageNumber=1&pageSize=8");

				// 2. Parse as a JSON Document to inspect it dynamically
				using var doc = System.Text.Json.JsonDocument.Parse(jsonString);
				var root = doc.RootElement;

				if (root.ValueKind == System.Text.Json.JsonValueKind.Array)
				{
					// Case A: API returns a simple list [ {...}, {...} ]
					featuredProducts = System.Text.Json.JsonSerializer.Deserialize<List<SanPhamKhachHangViewModel>>(jsonString, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
				}
				else if (root.ValueKind == System.Text.Json.JsonValueKind.Object)
				{
					// Case B: API returns an object { "total": 100, "items": [...] }
					// We search for the first property that is an array
					foreach (var property in root.EnumerateObject())
					{
						if (property.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
						{
							featuredProducts = System.Text.Json.JsonSerializer.Deserialize<List<SanPhamKhachHangViewModel>>(property.Value.GetRawText(), new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
							break;
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"API Error: {ex.Message}");
			}

			ViewBag.FeaturedProducts = featuredProducts;
			return View(banners);
		}

		public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
		public class ApiWrapper
		{
			public List<SanPhamKhachHangViewModel> Items { get; set; } // Or "Data", "Products" - check your API
		}
	}
}
