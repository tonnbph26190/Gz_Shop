using BanQuanAu1.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace QuanApi.Controllers;

/// <summary>API thống kê và biểu đồ cho dashboard admin.</summary>
[ApiController]
[Route("api/[controller]")]
public class ChartsApiController : ControllerBase
{
    private readonly BanQuanAu1DbContext _context;

    /// <summary>Trạng thái đơn hàng được coi là đã thanh toán / hoàn thành (dùng thống kê doanh thu).</summary>
    private static readonly string[] CompletedOrderStatuses = { "DaThanhToan", "Giao hàng thành công" };

    public ChartsApiController(BanQuanAu1DbContext context)
    {
        _context = context;
    }

    /// <summary>Tổng hợp KPIs dashboard (1 request thay vì nhiều).</summary>
    [HttpGet("dashboard-summary")]
    public async Task<IActionResult> GetDashboardSummary()
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var now = DateTime.UtcNow;
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            var todayRevenue = await _context.HoaDons
                .Where(h => CompletedOrderStatuses.Contains(h.TrangThai) && h.TrangThaiHoaDon && h.NgayTao.Date == today)
                .SumAsync(h => h.TongTien - (h.TienGiam ?? 0) - (h.PhiVanChuyen ?? 0));

            var monthRevenue = await _context.HoaDons
                .Where(h => CompletedOrderStatuses.Contains(h.TrangThai) && h.TrangThaiHoaDon &&
                            h.NgayTao >= firstDayOfMonth && h.NgayTao <= lastDayOfMonth)
                .SumAsync(h => h.TongTien - (h.TienGiam ?? 0) - (h.PhiVanChuyen ?? 0));

            var monthProductQuantity = await _context.ChiTietHoaDons
                .Include(ct => ct.HoaDon)
                .Where(ct => CompletedOrderStatuses.Contains(ct.HoaDon.TrangThai) && ct.HoaDon.TrangThaiHoaDon && ct.TrangThai &&
                             ct.HoaDon.NgayTao >= firstDayOfMonth && ct.HoaDon.NgayTao <= lastDayOfMonth)
                .SumAsync(ct => ct.SoLuong);

            var todayShipping = await _context.HoaDons
                .Where(h => CompletedOrderStatuses.Contains(h.TrangThai) && h.TrangThaiHoaDon && h.NgayTao.Date == today &&
                            h.PhiVanChuyen.HasValue && h.PhiVanChuyen > 0)
                .Select(h => new { h.PhiVanChuyen })
                .ToListAsync();
            var todayShippingCount = todayShipping.Count;
            var todayShippingTotal = todayShipping.Sum(h => h.PhiVanChuyen ?? 0);

            var monthShipping = await _context.HoaDons
                .Where(h => CompletedOrderStatuses.Contains(h.TrangThai) && h.TrangThaiHoaDon &&
                            h.NgayTao >= firstDayOfMonth && h.NgayTao <= lastDayOfMonth &&
                            h.PhiVanChuyen.HasValue && h.PhiVanChuyen > 0)
                .Select(h => new { h.PhiVanChuyen })
                .ToListAsync();
            var monthShippingCount = monthShipping.Count;
            var monthShippingTotal = monthShipping.Sum(h => h.PhiVanChuyen ?? 0);

            var totalProducts = await _context.SanPhams.CountAsync();
            var totalQuantity = await _context.SanPhamChiTiets.SumAsync(spct => spct.SoLuong);
            var outOfStockProducts = await _context.SanPhamChiTiets
                .GroupBy(spct => spct.IDSanPham)
                .Where(g => g.Sum(spct => spct.SoLuong) == 0)
                .CountAsync();
            var lowStockProducts = await _context.SanPhamChiTiets
                .GroupBy(spct => spct.IDSanPham)
                .Where(g => g.Sum(spct => spct.SoLuong) > 0 && g.Sum(spct => spct.SoLuong) <= 10)
                .CountAsync();

            return Ok(new
            {
                success = true,
                data = new
                {
                    todayRevenue,
                    monthRevenue,
                    monthProductQuantity,
                    todayShippingCount,
                    todayShippingTotal,
                    monthShippingCount,
                    monthShippingTotal,
                    totalProducts,
                    totalQuantity,
                    outOfStockProducts,
                    lowStockProducts
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("today-revenue")]
    public async Task<IActionResult> GetTodayRevenue()
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var revenue = await _context.HoaDons
                .Where(h => CompletedOrderStatuses.Contains(h.TrangThai) && h.TrangThaiHoaDon && h.NgayTao.Date == today)
                .SumAsync(h => h.TongTien - (h.TienGiam ?? 0) - (h.PhiVanChuyen ?? 0));
            return Ok(revenue);
        }
        catch (Exception ex) { return BadRequest(new { success = false, message = ex.Message }); }
    }

    [HttpGet("month-revenue")]
    public async Task<IActionResult> GetMonthRevenue()
    {
        try
        {
            var now = DateTime.UtcNow;
            var first = new DateTime(now.Year, now.Month, 1);
            var last = first.AddMonths(1).AddDays(-1);
            var revenue = await _context.HoaDons
                .Where(h => CompletedOrderStatuses.Contains(h.TrangThai) && h.TrangThaiHoaDon &&
                            h.NgayTao >= first && h.NgayTao <= last)
                .SumAsync(h => h.TongTien - (h.TienGiam ?? 0) - (h.PhiVanChuyen ?? 0));
            return Ok(revenue);
        }
        catch (Exception ex) { return BadRequest(new { success = false, message = ex.Message }); }
    }

    [HttpGet("month-product-quantity")]
    public async Task<IActionResult> GetMonthProductQuantity()
    {
        try
        {
            var now = DateTime.UtcNow;
            var first = new DateTime(now.Year, now.Month, 1);
            var last = first.AddMonths(1).AddDays(-1);
            var total = await _context.ChiTietHoaDons
                .Include(ct => ct.HoaDon)
                .Where(ct => CompletedOrderStatuses.Contains(ct.HoaDon.TrangThai) && ct.HoaDon.TrangThaiHoaDon && ct.TrangThai &&
                             ct.HoaDon.NgayTao >= first && ct.HoaDon.NgayTao <= last)
                .SumAsync(ct => ct.SoLuong);
            return Ok(total);
        }
        catch (Exception ex) { return BadRequest(new { success = false, message = ex.Message }); }
    }

    [HttpGet("today-shipping-stats")]
    public async Task<IActionResult> GetTodayShippingStats()
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var query = _context.HoaDons
                .Where(h => CompletedOrderStatuses.Contains(h.TrangThai) && h.TrangThaiHoaDon && h.NgayTao.Date == today &&
                            h.PhiVanChuyen.HasValue && h.PhiVanChuyen > 0);
            var count = await query.CountAsync();
            var totalShipping = await query.SumAsync(h => h.PhiVanChuyen ?? 0);
            return Ok(new { count, totalShipping });
        }
        catch (Exception ex) { return BadRequest(new { success = false, message = ex.Message }); }
    }

    [HttpGet("month-shipping-stats")]
    public async Task<IActionResult> GetMonthShippingStats()
    {
        try
        {
            var now = DateTime.UtcNow;
            var first = new DateTime(now.Year, now.Month, 1);
            var last = first.AddMonths(1).AddDays(-1);
            var query = _context.HoaDons
                .Where(h => CompletedOrderStatuses.Contains(h.TrangThai) && h.TrangThaiHoaDon &&
                            h.NgayTao >= first && h.NgayTao <= last &&
                            h.PhiVanChuyen.HasValue && h.PhiVanChuyen > 0);
            var count = await query.CountAsync();
            var totalShipping = await query.SumAsync(h => h.PhiVanChuyen ?? 0);
            return Ok(new { count, totalShipping });
        }
        catch (Exception ex) { return BadRequest(new { success = false, message = ex.Message }); }
    }

    [HttpGet("inventory-summary")]
    public async Task<IActionResult> GetInventorySummary()
    {
        try
        {
            var totalProducts = await _context.SanPhams.CountAsync();
            var totalVariants = await _context.SanPhamChiTiets.CountAsync();
            var totalQuantity = await _context.SanPhamChiTiets.SumAsync(spct => spct.SoLuong);
            var outOfStockProducts = await _context.SanPhamChiTiets
                .GroupBy(spct => spct.IDSanPham)
                .Where(g => g.Sum(spct => spct.SoLuong) == 0)
                .CountAsync();
            var lowStockProducts = await _context.SanPhamChiTiets
                .GroupBy(spct => spct.IDSanPham)
                .Where(g => g.Sum(spct => spct.SoLuong) > 0 && g.Sum(spct => spct.SoLuong) <= 10)
                .CountAsync();
            return Ok(new
            {
                success = true,
                data = new
                {
                    totalProducts,
                    totalVariants,
                    totalQuantity,
                    outOfStockProducts,
                    lowStockProducts
                }
            });
        }
        catch (Exception ex) { return BadRequest(new { success = false, message = ex.Message }); }
    }

    [HttpGet("area-chart-data")]
    public async Task<IActionResult> GetAreaChartData()
    {
        try
        {
            var startDate = DateTime.UtcNow.AddMonths(-11).Date;
            var endDate = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);

            var monthlyData = await _context.HoaDons
                .Where(h => CompletedOrderStatuses.Contains(h.TrangThai) && h.TrangThaiHoaDon &&
                            h.NgayTao >= startDate && h.NgayTao <= endDate)
                .GroupBy(h => new { h.NgayTao.Year, h.NgayTao.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Revenue = g.Sum(h => h.TongTien - (h.TienGiam ?? 0) - (h.PhiVanChuyen ?? 0)),
                    OrderCount = g.Count()
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            var labels = new List<string>();
            var revenues = new List<decimal>();
            for (var i = 11; i >= 0; i--)
            {
                var d = DateTime.UtcNow.AddMonths(-i);
                var m = monthlyData.FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month);
                labels.Add($"Tháng {d.Month}/{d.Year}");
                revenues.Add(m?.Revenue ?? 0);
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    labels,
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
                }
            });
        }
        catch (Exception ex) { return BadRequest(new { success = false, message = ex.Message }); }
    }

    [HttpGet("bar-chart-data")]
    public async Task<IActionResult> GetBarChartData()
    {
        try
        {
            var from = DateTime.UtcNow.AddMonths(-3);
            var topProducts = await _context.ChiTietHoaDons
                .Include(ct => ct.SanPhamChiTiet).ThenInclude(spct => spct.SanPham)
                .Include(ct => ct.HoaDon)
                .Where(ct => CompletedOrderStatuses.Contains(ct.HoaDon.TrangThai) && ct.HoaDon.TrangThaiHoaDon && ct.TrangThai &&
                             ct.HoaDon.NgayTao >= from)
                .GroupBy(ct => new { ct.SanPhamChiTiet.SanPham.IDSanPham, ct.SanPhamChiTiet.SanPham.TenSanPham })
                .Select(g => new
                {
                    g.Key.TenSanPham,
                    TotalSold = g.Sum(ct => ct.SoLuong),
                    TotalRevenue = g.Sum(ct => ct.ThanhTien)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(10)
                .ToListAsync();

            var labels = topProducts.Select(p => (p.TenSanPham ?? "").Length > 20 ? (p.TenSanPham ?? "").Substring(0, 20) + "..." : p.TenSanPham ?? "").ToList();
            var soldData = topProducts.Select(p => p.TotalSold).ToList();
            var colors = new[] { "rgba(54,162,235,0.8)", "rgba(255,99,132,0.8)", "rgba(255,205,86,0.8)", "rgba(75,192,192,0.8)", "rgba(153,102,255,0.8)", "rgba(255,159,64,0.8)", "rgba(199,199,199,0.8)", "rgba(83,102,255,0.8)", "rgba(255,99,255,0.8)", "rgba(99,255,132,0.8)" };
            var borderColors = new[] { "rgba(54,162,235,1)", "rgba(255,99,132,1)", "rgba(255,205,86,1)", "rgba(75,192,192,1)", "rgba(153,102,255,1)", "rgba(255,159,64,1)", "rgba(199,199,199,1)", "rgba(83,102,255,1)", "rgba(255,99,255,1)", "rgba(99,255,132,1)" };

            return Ok(new
            {
                success = true,
                data = new
                {
                    labels,
                    datasets = new[]
                    {
                        new
                        {
                            label = "Số lượng bán",
                            data = soldData,
                            backgroundColor = colors.Take(labels.Count).ToArray(),
                            borderColor = borderColors.Take(labels.Count).ToArray(),
                            borderWidth = 1
                        }
                    }
                }
            });
        }
        catch (Exception ex) { return BadRequest(new { success = false, message = ex.Message }); }
    }

    [HttpGet("pie-chart-data")]
    public async Task<IActionResult> GetPieChartData()
    {
        try
        {
            var from = DateTime.UtcNow.AddMonths(-3);
            var totalRevenue = await _context.ChiTietHoaDons
                .Include(ct => ct.HoaDon)
                .Where(ct => CompletedOrderStatuses.Contains(ct.HoaDon.TrangThai) && ct.HoaDon.TrangThaiHoaDon && ct.TrangThai &&
                             ct.HoaDon.NgayTao >= from)
                .SumAsync(ct => ct.ThanhTien);

            if (totalRevenue == 0)
            {
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        labels = new[] { "Không có dữ liệu" },
                        datasets = new[] { new { data = new[] { 1m }, backgroundColor = new[] { "rgba(200,200,200,0.8)" }, borderColor = new[] { "rgba(200,200,200,1)" }, borderWidth = 1 } }
                    }
                });
            }

            var categoryData = await _context.ChiTietHoaDons
                .Include(ct => ct.SanPhamChiTiet).ThenInclude(spct => spct.SanPham).ThenInclude(sp => sp.DanhMuc)
                .Include(ct => ct.HoaDon)
                .Where(ct => CompletedOrderStatuses.Contains(ct.HoaDon.TrangThai) && ct.HoaDon.TrangThaiHoaDon && ct.TrangThai &&
                             ct.HoaDon.NgayTao >= from)
                .GroupBy(ct => ct.SanPhamChiTiet.SanPham.DanhMuc != null ? ct.SanPhamChiTiet.SanPham.DanhMuc.TenDanhMuc : null)
                .Select(g => new { CategoryName = g.Key ?? "Không xác định", Revenue = g.Sum(ct => ct.ThanhTien) })
                .OrderByDescending(x => x.Revenue)
                .ToListAsync();

            var labels = categoryData.Select(c => c.CategoryName).ToList();
            var data = categoryData.Select(c => c.Revenue).ToList();
            var colors = new[] { "rgba(255,99,132,0.8)", "rgba(54,162,235,0.8)", "rgba(255,205,86,0.8)", "rgba(75,192,192,0.8)", "rgba(153,102,255,0.8)", "rgba(255,159,64,0.8)", "rgba(199,199,199,0.8)", "rgba(83,102,255,0.8)", "rgba(255,99,255,0.8)", "rgba(99,255,132,0.8)" };
            var borderColors = new[] { "rgba(255,99,132,1)", "rgba(54,162,235,1)", "rgba(255,205,86,1)", "rgba(75,192,192,1)", "rgba(153,102,255,1)", "rgba(255,159,64,1)", "rgba(199,199,199,1)", "rgba(83,102,255,1)", "rgba(255,99,255,1)", "rgba(99,255,132,1)" };

            return Ok(new
            {
                success = true,
                data = new
                {
                    labels,
                    datasets = new[]
                    {
                        new
                        {
                            data,
                            backgroundColor = colors.Take(labels.Count).ToArray(),
                            borderColor = borderColors.Take(labels.Count).ToArray(),
                            borderWidth = 2,
                            cutout = "50%"
                        }
                    }
                }
            });
        }
        catch (Exception ex) { return BadRequest(new { success = false, message = ex.Message }); }
    }

    [HttpGet("trang-thai-don-hang-trong-thang")]
    public async Task<IActionResult> GetTrangThaiDonHangTrongThang()
    {
        var now = DateTime.UtcNow;
        var first = new DateTime(now.Year, now.Month, 1);
        var last = first.AddMonths(1).AddDays(-1);
        var data = await _context.HoaDons
            .Where(h => h.NgayTao >= first && h.NgayTao <= last && h.TrangThaiHoaDon)
            .GroupBy(h => h.TrangThai)
            .Select(g => new { TrangThai = g.Key, SoLuong = g.Count() })
            .ToListAsync();
        return Ok(data);
    }

    [HttpGet("product-category-inventory")]
    public async Task<IActionResult> GetProductCategoryInventory()
    {
        try
        {
            var list = await _context.SanPhamChiTiets
                .Include(spct => spct.SanPham).ThenInclude(sp => sp.DanhMuc)
                .GroupBy(spct => new { spct.SanPham!.DanhMuc!.IDDanhMuc, spct.SanPham.DanhMuc.TenDanhMuc })
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

            var labels = list.Select(c => c.CategoryName).ToList();
            var quantities = list.Select(c => c.TotalQuantity).ToList();
            var colors = new[] { "rgba(255,99,132,0.8)", "rgba(54,162,235,0.8)", "rgba(255,205,86,0.8)", "rgba(75,192,192,0.8)", "rgba(153,102,255,0.8)", "rgba(255,159,64,0.8)", "rgba(199,199,199,0.8)", "rgba(83,102,255,0.8)", "rgba(255,99,255,0.8)", "rgba(99,255,132,0.8)" };
            var borderColors = new[] { "rgba(255,99,132,1)", "rgba(54,162,235,1)", "rgba(255,205,86,1)", "rgba(75,192,192,1)", "rgba(153,102,255,1)", "rgba(255,159,64,1)", "rgba(199,199,199,1)", "rgba(83,102,255,1)", "rgba(255,99,255,1)", "rgba(99,255,132,1)" };

            return Ok(new
            {
                success = true,
                data = new
                {
                    labels,
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
                    details = list.Select(c => new { categoryName = c.CategoryName, totalQuantity = c.TotalQuantity, productCount = c.ProductCount, variantCount = c.VariantCount }).ToList()
                }
            });
        }
        catch (Exception ex) { return BadRequest(new { success = false, message = ex.Message }); }
    }

    [HttpGet("SanPhamHetHang")]
    public async Task<IActionResult> GetSanPhamHetHang()
    {
        var list = await _context.SanPhamChiTiets
            .GroupBy(ct => ct.IDSanPham)
            .Where(g => g.Sum(ct => ct.SoLuong) == 0)
            .Join(_context.SanPhams, g => g.Key, sp => sp.IDSanPham, (g, sp) => new { sp.IDSanPham, sp.MaSanPham, sp.TenSanPham })
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("summary-stats")]
    public async Task<IActionResult> GetSummaryStats()
    {
        try
        {
            var now = DateTime.UtcNow;
            var first = new DateTime(now.Year, now.Month, 1);
            var last = first.AddMonths(1).AddDays(-1);
            var today = DateTime.UtcNow.Date;

            var monthlyRevenue = await _context.HoaDons
                .Where(h => CompletedOrderStatuses.Contains(h.TrangThai) && h.TrangThaiHoaDon && h.NgayTao >= first && h.NgayTao <= last)
                .SumAsync(h => h.TongTien - (h.TienGiam ?? 0) - (h.PhiVanChuyen ?? 0));

            var todayOrders = await _context.HoaDons.CountAsync(h => h.NgayTao.Date == today && h.TrangThaiHoaDon);
            var totalCustomers = await _context.KhachHang.CountAsync(k => k.TrangThai);
            var pendingOrders = await _context.HoaDons.CountAsync(h => h.TrangThai == "Chờ xác nhận" && h.TrangThaiHoaDon);

            return Ok(new
            {
                success = true,
                data = new
                {
                    monthly_revenue = monthlyRevenue,
                    today_orders = todayOrders,
                    total_customers = totalCustomers,
                    pending_orders = pendingOrders
                }
            });
        }
        catch (Exception ex) { return BadRequest(new { success = false, message = ex.Message }); }
    }
}
