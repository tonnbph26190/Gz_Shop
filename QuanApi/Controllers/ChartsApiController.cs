using BanQuanAu1.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;

[ApiController]
[Route("api/[controller]")]
public class ChartsApiController : ControllerBase
{
    private readonly BanQuanAu1DbContext _context;

    public ChartsApiController(BanQuanAu1DbContext context)
    {
        _context = context;
    }

    // API cho Area Chart - Doanh thu theo tháng (12 tháng gần nhất)
    [HttpGet("area-chart-data")]
    public async Task<IActionResult> GetAreaChartData()
    {
        try
        {
            var startDate = DateTime.Now.AddMonths(-11).Date; // 12 tháng gần nhất
            var endDate = DateTime.Now.Date.AddDays(1).AddTicks(-1); // Cuối ngày hôm nay

            var monthlyData = await _context.HoaDons
                .Where(h => h.TrangThai == "DaThanhToan" || h.TrangThai == "Giao hàng thành công" &&
                            h.TrangThaiHoaDon == true &&
                            h.NgayTao >= startDate && h.NgayTao <= endDate)
                .GroupBy(h => new { h.NgayTao.Year, h.NgayTao.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(h => h.TongTien - (h.TienGiam ?? 0) - (h.PhiVanChuyen ?? 0)),
                    OrderCount = g.Count()
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            var chartData = new List<object>();
            var labels = new List<string>();
            var revenues = new List<decimal>();
            var orderCounts = new List<int>();

            for (int i = 11; i >= 0; i--)
            {
                var targetDate = DateTime.Now.AddMonths(-i);
                var monthData = monthlyData.FirstOrDefault(x => x.Year == targetDate.Year && x.Month == targetDate.Month);

                labels.Add($"Tháng {targetDate.Month}/{targetDate.Year}");
                revenues.Add(monthData?.Revenue ?? 0);
                orderCounts.Add(monthData?.OrderCount ?? 0);
            }

            var result = new
            {
                labels = labels,
                datasets = new[]
                {
                new
                {
                    label = "Doanh thu (VNĐ)",
                    data = revenues,
                    backgroundColor = "rgba(78, 115, 223, 0.1)",
                    borderColor = "rgba(78, 115, 223, 1)",
                    borderWidth = 2,
                    fill = true,
                    tension = 0.3
                }
            }
            };

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }


    //[HttpGet("area-chart-data")]
    //public IActionResult GetAreaChartData()
    //{
    //    var mockData = new
    //    {
    //        success = true,
    //        data = new
    //        {
    //            labels = new[] { "1", "2", "3", "4", "5", "6", "7" }, // 7 tháng đầu năm
    //            datasets = new[]
    //            {
    //            new {
    //                label = "Doanh thu",
    //                data = new[] { 5000000, 7000000, 4500000, 8000000, 12000000, 10000000, 9500000 }
    //            }
    //        }
    //        }
    //    };
    //    return Ok(mockData);
    //}


    //API cho Bar Chart - Top 10 sản phẩm bán chạy
    [HttpGet("bar-chart-data")]
    public async Task<IActionResult> GetBarChartData()
    {
        try
        {
            var topProducts = await _context.ChiTietHoaDons
                .Include(ct => ct.SanPhamChiTiet)
                    .ThenInclude(spct => spct.SanPham)
                .Include(ct => ct.HoaDon)
                .Where(ct => ct.HoaDon.TrangThai == "DaThanhToan" || ct.HoaDon.TrangThai == "Giao hàng thành công" &&
                           ct.HoaDon.TrangThaiHoaDon == true &&
                           ct.TrangThai == true &&
                           ct.HoaDon.NgayTao >= DateTime.Now.AddMonths(-3)) // 3 tháng gần nhất
                .GroupBy(ct => new
                {
                    ct.SanPhamChiTiet.SanPham.IDSanPham,
                    ct.SanPhamChiTiet.SanPham.TenSanPham
                })
                .Select(g => new
                {
                    ProductName = g.Key.TenSanPham,
                    TotalSold = g.Sum(ct => ct.SoLuong),
                    TotalRevenue = g.Sum(ct => ct.ThanhTien)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(10)
                .ToListAsync();

            var labels = topProducts.Select(p => p.ProductName.Length > 20 ?
                p.ProductName.Substring(0, 20) + "..." : p.ProductName).ToList();
            var soldData = topProducts.Select(p => p.TotalSold).ToList();
            var revenueData = topProducts.Select(p => p.TotalRevenue).ToList();

            var result = new
            {
                labels = labels,
                datasets = new[]
                {
                    new
                    {
                        label = "Số lượng bán",
                        data = soldData,
                        backgroundColor = new[]
                        {
                            "rgba(54, 162, 235, 0.8)",
                            "rgba(255, 99, 132, 0.8)",
                            "rgba(255, 205, 86, 0.8)",
                            "rgba(75, 192, 192, 0.8)",
                            "rgba(153, 102, 255, 0.8)",
                            "rgba(255, 159, 64, 0.8)",
                            "rgba(199, 199, 199, 0.8)",
                            "rgba(83, 102, 255, 0.8)",
                            "rgba(255, 99, 255, 0.8)",
                            "rgba(99, 255, 132, 0.8)"
                        },
                        borderColor = new[]
                        {
                            "rgba(54, 162, 235, 1)",
                            "rgba(255, 99, 132, 1)",
                            "rgba(255, 205, 86, 1)",
                            "rgba(75, 192, 192, 1)",
                            "rgba(153, 102, 255, 1)",
                            "rgba(255, 159, 64, 1)",
                            "rgba(199, 199, 199, 1)",
                            "rgba(83, 102, 255, 1)",
                            "rgba(255, 99, 255, 1)",
                            "rgba(99, 255, 132, 1)"
                        },
                        borderWidth = 1
                    }
                }
            };

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    //[HttpGet("bar-chart-data")]
    //public IActionResult GetBarChartData()
    //{
    //    var mockData = new
    //    {
    //        success = true,
    //        data = new
    //        {
    //            labels = new[] { "1", "2", "3", "4", "5", "6" },
    //            datasets = new[]
    //            {
    //            new {
    //                label = "Sản phẩm bán",
    //                data = new[] { 100, 120, 90, 150, 170, 200 }
    //            }
    //        }
    //        }
    //    };

    //    return Ok(mockData);
    //}


    //API cho Donut Chart - Phân bố doanh thu theo danh mục
    [HttpGet("pie-chart-data")]
    public async Task<IActionResult> GetPieChartData()
    {
        try
        {
            var totalRevenue = await _context.ChiTietHoaDons
                .Include(ct => ct.HoaDon)
                .Where(ct => ct.HoaDon.TrangThai == "DaThanhToan" || ct.HoaDon.TrangThai == "Giao hàng thành công" &&
                           ct.HoaDon.TrangThaiHoaDon == true &&
                           ct.HoaDon.NgayTao >= DateTime.Now.AddMonths(-3) &&
                           ct.TrangThai == true)
                .SumAsync(ct => ct.ThanhTien);

            if (totalRevenue == 0)
            {
                var emptyResult = new
                {
                    labels = new[] { "Không có dữ liệu" },
                    datasets = new[]
                    {
                        new
                        {
                            data = new[] { 1 },
                            backgroundColor = new[] { "rgba(200, 200, 200, 0.8)" },
                            borderColor = new[] { "rgba(200, 200, 200, 1)" },
                            borderWidth = 1
                        }
                    }
                };
                return Ok(new { success = true, data = emptyResult });
            }

            var categoryData = await _context.ChiTietHoaDons
                .Include(ct => ct.SanPhamChiTiet)
                    .ThenInclude(spct => spct.SanPham)
                        .ThenInclude(sp => sp.DanhMuc)
                .Include(ct => ct.HoaDon)
                .Where(ct => ct.HoaDon.TrangThai == "DaThanhToan" || ct.HoaDon.TrangThai == "Giao hàng thành công" &&
                           ct.HoaDon.TrangThaiHoaDon == true &&
                           ct.HoaDon.NgayTao >= DateTime.Now.AddMonths(-3) &&
                           ct.TrangThai == true)
                .GroupBy(ct => ct.SanPhamChiTiet.SanPham.DanhMuc.TenDanhMuc)
                .Select(g => new
                {
                    CategoryName = g.Key ?? "Không xác định",
                    Revenue = g.Sum(ct => ct.ThanhTien),
                    Percentage = Math.Round((double)g.Sum(ct => ct.ThanhTien) / (double)totalRevenue * 100, 1)
                })
                .OrderByDescending(x => x.Revenue)
                .ToListAsync();

            var labels = categoryData.Select(c => c.CategoryName).ToList();
            var data = categoryData.Select(c => c.Revenue).ToList();
            var percentages = categoryData.Select(c => c.Percentage).ToList();

            // Màu sắc cho pie chart
            var colors = new[]
            {
                "rgba(255, 99, 132, 0.8)",
                "rgba(54, 162, 235, 0.8)",
                "rgba(255, 205, 86, 0.8)",
                "rgba(75, 192, 192, 0.8)",
                "rgba(153, 102, 255, 0.8)",
                "rgba(255, 159, 64, 0.8)",
                "rgba(199, 199, 199, 0.8)",
                "rgba(83, 102, 255, 0.8)",
                "rgba(255, 99, 255, 0.8)",
                "rgba(99, 255, 132, 0.8)"
            };

            var borderColors = new[]
            {
                "rgba(255, 99, 132, 1)",
                "rgba(54, 162, 235, 1)",
                "rgba(255, 205, 86, 1)",
                "rgba(75, 192, 192, 1)",
                "rgba(153, 102, 255, 1)",
                "rgba(255, 159, 64, 1)",
                "rgba(199, 199, 199, 1)",
                "rgba(83, 102, 255, 1)",
                "rgba(255, 99, 255, 1)",
                "rgba(99, 255, 132, 1)"
            };

            var result = new
            {
                labels = labels,
                datasets = new[]
                {
                    new
                    {
                        data = data,
                        backgroundColor = colors.Take(labels.Count).ToArray(),
                        borderColor = borderColors.Take(labels.Count).ToArray(),
                        borderWidth = 2,
                        cutout = "50%" // Tạo donut chart
                    }
                },
                // Thêm thông tin phần trăm để hiển thị tooltip
                percentages = percentages
            };

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    //[HttpGet("pie-chart-data")]
    //public IActionResult GetPieChartData()
    //{
    //    var mockData = new
    //    {
    //        success = true,
    //        data = new
    //        {
    //            labels = new[] { "Áo", "Quần", "Giày", "Phụ kiện" },
    //            datasets = new[]
    //            {
    //            new {
    //                data = new[] { 40, 30, 20, 10 },
    //                backgroundColor = new[]
    //                {
    //                    "#4e73df", "#1cc88a", "#36b9cc", "#f6c23e"
    //                },
    //                hoverBackgroundColor = new[]
    //                {
    //                    "#2e59d9", "#17a673", "#2c9faf", "#f4b619"
    //                },
    //                hoverBorderColor = "rgba(234, 236, 244, 1)"
    //            }
    //        }
    //        }
    //    };

    //    return Ok(mockData);
    //}

    [HttpGet("trang-thai-don-hang-trong-thang")]
    public async Task<IActionResult> GetTrangThaiDonHangTrongThang()
    {
        var now = DateTime.Now;
        var firstDay = new DateTime(now.Year, now.Month, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);

        var data = await _context.HoaDons
            .Where(h => h.NgayTao >= firstDay && h.NgayTao <= lastDay && h.TrangThaiHoaDon)
            .GroupBy(h => h.TrangThai)
            .Select(g => new
            {
                TrangThai = g.Key,
                SoLuong = g.Count()
            })
            .ToListAsync();

        return Ok(data);
    }

    //sản phẩm hết hàng 
    [HttpGet("SanPhamHetHang")]
    public async Task<IActionResult> GetSanPhamHetHang()
    {
        var sanPhamHetHang = await _context.SanPhamChiTiets
            .GroupBy(ct => ct.IDSanPham)
            .Select(group => new
            {
                IDSanPham = group.Key,
                SoLuongTon = group.Sum(ct => ct.SoLuong),
            })
            .Where(x => x.SoLuongTon == 0)
            .Join(_context.SanPhams,
                  g => g.IDSanPham,
                  sp => sp.IDSanPham,
                  (g, sp) => new
                  {
                      sp.IDSanPham,
                      sp.MaSanPham,
                      sp.TenSanPham
                  })
            .ToListAsync();

        return Ok(sanPhamHetHang);
    }

    // API cho doanh thu hôm nay (khong bao gồm phí vận chuyển)
    [HttpGet("today-revenue")]
    public async Task<IActionResult> GetTodayRevenue()
    {
        try
        {
            var today = DateTime.Today;
            var todayRevenue = await _context.HoaDons
                .Where(h => (h.TrangThai == "DaThanhToan" || h.TrangThai == "Giao hàng thành công") &&
                            h.TrangThaiHoaDon == true &&
                            h.NgayTao.Date == today)
                .SumAsync(h => h.TongTien - (h.TienGiam ?? 0) - (h.PhiVanChuyen ?? 0));

            return Ok(todayRevenue);
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // API cho doanh thu tháng này (khong bao gồm phí vận chuyển)
    [HttpGet("month-revenue")]
    public async Task<IActionResult> GetMonthRevenue()
    {
        try
        {
            var now = DateTime.Now;
            var firstDay = new DateTime(now.Year, now.Month, 1);
            var lastDay = firstDay.AddMonths(1).AddDays(-1);

            var monthRevenue = await _context.HoaDons
                .Where(h => h.TrangThai == "DaThanhToan" || h.TrangThai == "Giao hàng thành công" &&
                           h.TrangThaiHoaDon == true &&
                           h.NgayTao >= firstDay && h.NgayTao <= lastDay)
                .SumAsync(h => h.TongTien - (h.TienGiam ?? 0) - (h.PhiVanChuyen ?? 0));

            return Ok(monthRevenue);
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // API cho số lượng sản phẩm đã bán trong tháng
    [HttpGet("month-product-quantity")]
    public async Task<IActionResult> GetMonthProductQuantity()
    {
        try
        {
            var now = DateTime.Now;
            var firstDay = new DateTime(now.Year, now.Month, 1);
            var lastDay = firstDay.AddMonths(1).AddDays(-1);

            var totalQuantity = await _context.ChiTietHoaDons
                .Include(ct => ct.HoaDon)
                .Where(ct => ct.HoaDon.TrangThai == "DaThanhToan" || ct.HoaDon.TrangThai == "Giao hàng thành công" &&
                            ct.HoaDon.TrangThaiHoaDon == true &&
                            ct.TrangThai == true &&
                            ct.HoaDon.NgayTao >= firstDay &&
                            ct.HoaDon.NgayTao <= lastDay)
                .SumAsync(ct => ct.SoLuong);

            return Ok(totalQuantity);
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }


    // API tổng hợp tất cả dữ liệu biểu đồ (để giảm số lần gọi API)
    [HttpGet("all-charts-data")]
    public async Task<IActionResult> GetAllChartsData()
    {
        try
        {
            var areaData = await GetAreaChartDataInternal();
            var barData = await GetBarChartDataInternal();
            var donutData = await GetPieChartDataInternal();

            var result = new
            {
                areaChart = areaData,
                barChart = barData,
                donutChart = donutData
            };

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // Các phương thức internal để tái sử dụng code
    private async Task<object> GetAreaChartDataInternal()
    {
        // Copy logic từ GetAreaChartData() nhưng return object thay vì IActionResult
        var startDate = DateTime.Now.AddMonths(-11).Date;
        var monthlyData = await _context.HoaDons
            .Where(h => h.TrangThai == "Hoàn thành" && h.TrangThaiHoaDon == true && h.NgayTao >= startDate)
            .GroupBy(h => new { h.NgayTao.Year, h.NgayTao.Month })
            .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Revenue = g.Sum(h => h.TongTien - (h.TienGiam ?? 0) - (h.PhiVanChuyen ?? 0)) })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync();

        var labels = new List<string>();
        var revenues = new List<decimal>();

        for (int i = 11; i >= 0; i--)
        {
            var targetDate = DateTime.Now.AddMonths(-i);
            var monthData = monthlyData.FirstOrDefault(x => x.Year == targetDate.Year && x.Month == targetDate.Month);

            labels.Add($"Tháng {targetDate.Month}/{targetDate.Year}");
            revenues.Add(monthData?.Revenue ?? 0);
        }

        return new { labels, datasets = new[] { new { label = "Doanh thu (VNĐ)", data = revenues } } };
    }

    private async Task<object> GetBarChartDataInternal()
    {
        var topProducts = await _context.ChiTietHoaDons
            .Include(ct => ct.SanPhamChiTiet).ThenInclude(spct => spct.SanPham)
            .Include(ct => ct.HoaDon)
            .Where(ct => ct.HoaDon.TrangThai == "Hoàn thành" && ct.HoaDon.TrangThaiHoaDon == true && ct.TrangThai == true)
            .GroupBy(ct => ct.SanPhamChiTiet.SanPham.TenSanPham)
            .Select(g => new { ProductName = g.Key, TotalSold = g.Sum(ct => ct.SoLuong) })
            .OrderByDescending(x => x.TotalSold).Take(10).ToListAsync();

        return new
        {
            labels = topProducts.Select(p => p.ProductName).ToList(),
            datasets = new[] { new { label = "Số lượng bán", data = topProducts.Select(p => p.TotalSold).ToList() } }
        };
    }

    private async Task<object> GetPieChartDataInternal()
    {
        var categoryData = await _context.ChiTietHoaDons
            .Include(ct => ct.SanPhamChiTiet).ThenInclude(spct => spct.SanPham).ThenInclude(sp => sp.DanhMuc)
            .Include(ct => ct.HoaDon)
            .Where(ct => ct.HoaDon.TrangThai == "Hoàn thành" && ct.HoaDon.TrangThaiHoaDon == true && ct.TrangThai == true)
            .GroupBy(ct => ct.SanPhamChiTiet.SanPham.DanhMuc.TenDanhMuc)
            .Select(g => new { CategoryName = g.Key ?? "Không xác định", Revenue = g.Sum(ct => ct.ThanhTien) })
            .OrderByDescending(x => x.Revenue).ToListAsync();

        return new
        {
            labels = categoryData.Select(c => c.CategoryName).ToList(),
            datasets = new[] { new { data = categoryData.Select(c => c.Revenue).ToList() } }
        };
    }

    // Giữ lại các API khác từ code gốc...
    [HttpGet("summary-stats")]
    public async Task<IActionResult> GetSummaryStats()
    {
        try
        {
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            var today = DateTime.Today;

            var monthlyRevenue = await _context.HoaDons
                .Where(h => h.TrangThai == "Hoàn thành" &&
                           h.TrangThaiHoaDon == true &&
                           h.NgayTao.Month == currentMonth &&
                           h.NgayTao.Year == currentYear)
                .SumAsync(h => h.TongTien - (h.TienGiam ?? 0) - (h.PhiVanChuyen ?? 0));

            var todayOrders = await _context.HoaDons
                .Where(h => h.NgayTao.Date == today && h.TrangThaiHoaDon == true)
                .CountAsync();

            var totalCustomers = await _context.KhachHang
                .Where(k => k.TrangThai == true)
                .CountAsync();

            var pendingOrders = await _context.HoaDons
                .Where(h => h.TrangThai == "Chờ xác nhận" && h.TrangThaiHoaDon == true)
                .CountAsync();

            var stats = new
            {
                monthly_revenue = monthlyRevenue,
                today_orders = todayOrders,
                total_customers = totalCustomers,
                pending_orders = pendingOrders
            };

            return Ok(new { success = true, data = stats });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // API cho thống kê phí vận chuyển trong ngày
    [HttpGet("today-shipping-stats")]
    public async Task<IActionResult> GetTodayShippingStats()
    {
        try
        {
            var today = DateTime.Today;
            var query = _context.HoaDons
                .Where(h => (h.TrangThai == "DaThanhToan" || h.TrangThai == "Giao hàng thành công")
                            && h.TrangThaiHoaDon == true
                            && h.NgayTao.Date == today
                            && h.PhiVanChuyen.HasValue && h.PhiVanChuyen > 0);

            var count = await query.CountAsync();
            var totalShipping = await query.SumAsync(h => h.PhiVanChuyen ?? 0);

            return Ok(new { count, totalShipping });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // API cho thống kê phí vận chuyển trong tháng
    [HttpGet("month-shipping-stats")]
    public async Task<IActionResult> GetMonthShippingStats()
    {
        try
        {
            var now = DateTime.Now;
            var firstDay = new DateTime(now.Year, now.Month, 1);
            var lastDay = firstDay.AddMonths(1).AddDays(-1);
            var query = _context.HoaDons
                .Where(h => (h.TrangThai == "DaThanhToan" || h.TrangThai == "Giao hàng thành công")
                            && h.TrangThaiHoaDon == true
                            && h.NgayTao >= firstDay && h.NgayTao <= lastDay
                            && h.PhiVanChuyen.HasValue && h.PhiVanChuyen > 0);

            var count = await query.CountAsync();
            var totalShipping = await query.SumAsync(h => h.PhiVanChuyen ?? 0);

            return Ok(new { count, totalShipping });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // API cho thống kê các loại sản phẩm trong kho theo danh mục
    [HttpGet("product-category-inventory")]
    public async Task<IActionResult> GetProductCategoryInventory()
    {
        try
        {
            var categoryInventory = await _context.SanPhamChiTiets
                .Include(spct => spct.SanPham)
                    .ThenInclude(sp => sp.DanhMuc)
                .GroupBy(spct => new { 
                    spct.SanPham.DanhMuc.IDDanhMuc,
                    spct.SanPham.DanhMuc.TenDanhMuc 
                })
                .Select(g => new
                {
                    CategoryId = g.Key.IDDanhMuc,
                    CategoryName = g.Key.TenDanhMuc ?? "Không xác định",
                    TotalQuantity = g.Sum(spct => spct.SoLuong),
                    ProductCount = g.Select(spct => spct.IDSanPham).Distinct().Count(),
                    VariantCount = g.Count()
                })
                .OrderByDescending(x => x.TotalQuantity)
                .ToListAsync();

            var labels = categoryInventory.Select(c => c.CategoryName).ToList();
            var quantities = categoryInventory.Select(c => c.TotalQuantity).ToList();
            var productCounts = categoryInventory.Select(c => c.ProductCount).ToList();

            // Màu sắc cho biểu đồ
            var colors = new[]
            {
                "rgba(255, 99, 132, 0.8)",
                "rgba(54, 162, 235, 0.8)",
                "rgba(255, 205, 86, 0.8)",
                "rgba(75, 192, 192, 0.8)",
                "rgba(153, 102, 255, 0.8)",
                "rgba(255, 159, 64, 0.8)",
                "rgba(199, 199, 199, 0.8)",
                "rgba(83, 102, 255, 0.8)",
                "rgba(255, 99, 255, 0.8)",
                "rgba(99, 255, 132, 0.8)"
            };

            var borderColors = new[]
            {
                "rgba(255, 99, 132, 1)",
                "rgba(54, 162, 235, 1)",
                "rgba(255, 205, 86, 1)",
                "rgba(75, 192, 192, 1)",
                "rgba(153, 102, 255, 1)",
                "rgba(255, 159, 64, 1)",
                "rgba(199, 199, 199, 1)",
                "rgba(83, 102, 255, 1)",
                "rgba(255, 99, 255, 1)",
                "rgba(99, 255, 132, 1)"
            };

            var result = new
            {
                labels = labels,
                datasets = new[]
                {
                    new
                    {
                        label = "Số lượng tồn kho",
                        data = quantities,
                        backgroundColor = colors.Take(labels.Count).ToArray(),
                        borderColor = borderColors.Take(labels.Count).ToArray(),
                        borderWidth = 2
                    }
                },
                details = categoryInventory.Select(c => new
                {
                    categoryName = c.CategoryName,
                    totalQuantity = c.TotalQuantity,
                    productCount = c.ProductCount,
                    variantCount = c.VariantCount
                }).ToList()
            };

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // API cho thống kê tổng quan kho hàng
    [HttpGet("inventory-summary")]
    public async Task<IActionResult> GetInventorySummary()
    {
        try
        {
            // Tổng số sản phẩm (bao gồm cả hoạt động và không hoạt động)
            var totalProducts = await _context.SanPhams.CountAsync();

            // Tổng số biến thể sản phẩm (bao gồm cả hoạt động và không hoạt động)
            var totalVariants = await _context.SanPhamChiTiets.CountAsync();

            // Tổng số lượng tồn kho (bao gồm cả hoạt động và không hoạt động)
            var totalQuantity = await _context.SanPhamChiTiets
                .SumAsync(spct => spct.SoLuong);

            // Sản phẩm hết hàng (bao gồm cả hoạt động và không hoạt động)
            var outOfStockProducts = await _context.SanPhamChiTiets
                .GroupBy(spct => spct.IDSanPham)
                .Where(g => g.Sum(spct => spct.SoLuong) == 0)
                .CountAsync();

            // Sản phẩm sắp hết hàng (bao gồm cả hoạt động và không hoạt động)
            var lowStockProducts = await _context.SanPhamChiTiets
                .GroupBy(spct => spct.IDSanPham)
                .Where(g => g.Sum(spct => spct.SoLuong) > 0 && g.Sum(spct => spct.SoLuong) <= 10)
                .CountAsync();

            var summary = new
            {
                totalProducts = totalProducts,
                totalVariants = totalVariants,
                totalQuantity = totalQuantity,
                outOfStockProducts = outOfStockProducts,
                lowStockProducts = lowStockProducts
            };

            return Ok(new { success = true, data = summary });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}