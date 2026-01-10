using BanQuanAu1.Web.Data;
using Microsoft.AspNetCore.Mvc;
using QuanView.Models;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using QuanApi.Dtos;

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
                var response = await _httpClient.GetAsync("SanPhamNguoiDungs?pageNumber=1&pageSize=8");
                if (response.IsSuccessStatusCode)
                {
                    featuredProducts = await response.Content.ReadFromJsonAsync<List<SanPhamKhachHangViewModel>>() ?? new List<SanPhamKhachHangViewModel>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi lấy sản phẩm: {ex.Message}");
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
    }
}
