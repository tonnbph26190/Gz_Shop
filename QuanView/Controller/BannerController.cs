using BanQuanAu1.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanView.Models;

namespace QuanView.Controllers
{
    public class BannerController : Controller
    {
        private readonly BanQuanAu1DbContext _context;

        public BannerController(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> SanPhamTheoLink(int id)
        {
            var banner = await _context.Banners.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
            if (banner == null)
            {
                return NotFound();
            }

            var productIds = await _context.BannerSanPhams
                .AsNoTracking()
                .Where(x => x.BannerId == id)
                .Select(x => x.IDSanPham)
                .ToListAsync();

            var products = await _context.SanPhams
                .AsNoTracking()
                .Where(sp => sp.TrangThai && productIds.Contains(sp.IDSanPham))
                .Select(sp => new BannerLinkProductItemVm
                {
                    ProductId = sp.IDSanPham,
                    DefaultVariantId = sp.SanPhamChiTiets!
                        .Where(ct => ct.TrangThai)
                        .OrderBy(ct => ct.GiaBan)
                        .Select(ct => (Guid?)ct.IDSanPhamChiTiet)
                        .FirstOrDefault(),
                    ProductCode = sp.MaSanPham,
                    ProductName = sp.TenSanPham,
                    MinPrice = sp.SanPhamChiTiets!
                        .Where(ct => ct.TrangThai)
                        .Select(ct => (decimal?)ct.GiaBan)
                        .OrderBy(x => x)
                        .FirstOrDefault(),
                    ThumbnailUrl = sp.SanPhamChiTiets!
                        .SelectMany(ct => ct.AnhSanPhams!)
                        .Where(img => img.TrangThai)
                        .OrderByDescending(img => img.LaAnhChinh)
                        .Select(img => img.UrlAnh)
                        .FirstOrDefault()
                })
                .OrderBy(x => x.ProductName)
                .ToListAsync();

            var vm = new BannerLinkProductListVm
            {
                BannerId = banner.Id,
                BannerTitle = banner.Title ?? $"Banner #{banner.Id}",
                Products = products
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> XemChiTiet(Guid id)
        {
            var defaultVariantId = await _context.SanPhamChiTiets
                .AsNoTracking()
                .Where(ct => ct.TrangThai && ct.IDSanPham == id)
                .OrderBy(ct => ct.GiaBan)
                .Select(ct => (Guid?)ct.IDSanPhamChiTiet)
                .FirstOrDefaultAsync();

            if (!defaultVariantId.HasValue)
            {
                return NotFound();
            }

            return RedirectToAction("Detail", "SanPhamNguoiDung", new { id = defaultVariantId.Value });
        }
    }
}
