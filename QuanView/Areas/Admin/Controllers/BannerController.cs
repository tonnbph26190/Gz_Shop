using BanQuanAu1.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;
using QuanView.Areas.Admin.Models;

namespace QuanView.Areas.Admin.Controllers
{
    [Area("Admin")]
    //[Authorize(Policy = "AdminPolicy")]
    public class BannerController : Controller
    {
        private readonly BanQuanAu1DbContext _context;
        private readonly IWebHostEnvironment _env;

        public BannerController(BanQuanAu1DbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Admin/Banner
        public async Task<IActionResult> Index()
        {
            var banners = await _context.Banners.ToListAsync();
            return View(banners);
        }

        // GET: Admin/Banner/Create
        public async Task<IActionResult> Create()
        {
            var vm = new BannerFormVm
            {
                AvailableProducts = await LoadProductOptionsAsync()
            };
            return View(vm);
        }

        // POST: Admin/Banner/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BannerFormVm vm, IFormFile ImageFile)
        {
            var model = new Banner
            {
                Title = vm.Title?.Trim()
            };

            if (ImageFile != null && ImageFile.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                var path = Path.Combine(_env.WebRootPath, "img/banner", fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                model.ImageUrl = "/img/banner/" + fileName;
            }

            _context.Add(model);
            await _context.SaveChangesAsync();
            model.Link = BuildBannerProductLink(model.Id);

            var selectedIds = (vm.SelectedProductIds ?? new List<Guid>())
                .Distinct()
                .ToList();

            if (selectedIds.Count > 0)
            {
                var validIds = await _context.SanPhams
                    .AsNoTracking()
                    .Where(sp => selectedIds.Contains(sp.IDSanPham))
                    .Select(sp => sp.IDSanPham)
                    .ToListAsync();

                var mappings = validIds.Select(productId => new BannerSanPham
                {
                    BannerId = model.Id,
                    IDSanPham = productId
                });
                _context.BannerSanPhams.AddRange(mappings);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Banner/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null)
                return NotFound();

            var selectedIds = await _context.BannerSanPhams
                .AsNoTracking()
                .Where(x => x.BannerId == id)
                .Select(x => x.IDSanPham)
                .ToListAsync();

            var vm = new BannerFormVm
            {
                Id = banner.Id,
                Title = banner.Title,
                ImageUrl = banner.ImageUrl,
                ProductLink = BuildBannerProductLink(banner.Id),
                SelectedProductIds = selectedIds,
                AvailableProducts = await LoadProductOptionsAsync()
            };

            return View(vm);
        }

        // POST: Admin/Banner/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BannerFormVm model, IFormFile ImageFile)
        {
            if (id <= 0)
            {
                id = model.Id;
            }

            var banner = await _context.Banners.FindAsync(id);
            if (banner == null)
                return NotFound();

            if (ImageFile != null && ImageFile.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                var path = Path.Combine(_env.WebRootPath, "img/banner", fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                banner.ImageUrl = "/img/banner/" + fileName;
            }

            banner.Title = model.Title?.Trim();
            banner.Link = BuildBannerProductLink(banner.Id);

            _context.Update(banner);

            var selectedIds = (model.SelectedProductIds ?? new List<Guid>())
                .Distinct()
                .ToHashSet();

            var existingMappings = await _context.BannerSanPhams
                .Where(x => x.BannerId == id)
                .ToListAsync();

            var removeMappings = existingMappings
                .Where(x => !selectedIds.Contains(x.IDSanPham))
                .ToList();
            if (removeMappings.Count > 0)
            {
                _context.BannerSanPhams.RemoveRange(removeMappings);
            }

            var existingIds = existingMappings.Select(x => x.IDSanPham).ToHashSet();
            var addIds = selectedIds.Where(x => !existingIds.Contains(x)).ToList();

            if (addIds.Count > 0)
            {
                var validIds = await _context.SanPhams
                    .AsNoTracking()
                    .Where(sp => addIds.Contains(sp.IDSanPham))
                    .Select(sp => sp.IDSanPham)
                    .ToListAsync();

                var addMappings = validIds.Select(productId => new BannerSanPham
                {
                    BannerId = id,
                    IDSanPham = productId
                });
                _context.BannerSanPhams.AddRange(addMappings);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        // GET: Admin/Banner/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var banner = await _context.Banners.FirstOrDefaultAsync(b => b.Id == id);
            if (banner == null)
                return NotFound();

            return View(banner);
        }
        // POST: Admin/Banner/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null)
                return NotFound();

            // ❗ Xoá file ảnh vật lý (nếu có)
            if (!string.IsNullOrEmpty(banner.ImageUrl))
            {
                var filePath = Path.Combine(
                    _env.WebRootPath,
                    banner.ImageUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString())
                );

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.Banners.Remove(banner);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        // GET: Admin/Banner/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var banner = await _context.Banners.FirstOrDefaultAsync(b => b.Id == id);
            if (banner == null)
                return NotFound();

            return View(banner);
        }

        // GET: Admin/Banner/GanSanPham/5
        public async Task<IActionResult> GanSanPham(int id, string? keyword = null)
        {
            var banner = await _context.Banners.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
            if (banner == null)
            {
                return NotFound();
            }

            var assignedProductIds = await _context.BannerSanPhams
                .AsNoTracking()
                .Where(x => x.BannerId == id)
                .Select(x => x.IDSanPham)
                .ToListAsync();

            var assignedProducts = await _context.SanPhams
                .AsNoTracking()
                .Where(sp => assignedProductIds.Contains(sp.IDSanPham))
                .Select(ToBannerProductItemVm())
                .OrderBy(x => x.ProductName)
                .ToListAsync();

            var availableQuery = _context.SanPhams
                .AsNoTracking()
                .Where(sp => sp.TrangThai && !assignedProductIds.Contains(sp.IDSanPham));

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                availableQuery = availableQuery.Where(sp =>
                    sp.TenSanPham.Contains(keyword) || sp.MaSanPham.Contains(keyword));
            }

            var availableProducts = await availableQuery
                .Select(ToBannerProductItemVm())
                .OrderBy(x => x.ProductName)
                .Take(100)
                .ToListAsync();

            var vm = new BannerSanPhamVm
            {
                BannerId = banner.Id,
                BannerTitle = banner.Title ?? $"Banner #{banner.Id}",
                ProductLink = BuildBannerProductLink(banner.Id),
                Keyword = keyword,
                AssignedProducts = assignedProducts,
                AvailableProducts = availableProducts
            };

            return View(vm);
        }

        // POST: Admin/Banner/ThemSanPham
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemSanPham(int bannerId, Guid sanPhamId)
        {
            var bannerExists = await _context.Banners.AnyAsync(b => b.Id == bannerId);
            var productExists = await _context.SanPhams.AnyAsync(sp => sp.IDSanPham == sanPhamId);
            if (!bannerExists || !productExists)
            {
                return NotFound();
            }

            var existed = await _context.BannerSanPhams
                .AnyAsync(x => x.BannerId == bannerId && x.IDSanPham == sanPhamId);

            if (!existed)
            {
                _context.BannerSanPhams.Add(new BannerSanPham
                {
                    BannerId = bannerId,
                    IDSanPham = sanPhamId
                });
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(GanSanPham), new { id = bannerId });
        }

        // POST: Admin/Banner/XoaSanPham
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XoaSanPham(int bannerId, Guid sanPhamId)
        {
            var item = await _context.BannerSanPhams
                .FirstOrDefaultAsync(x => x.BannerId == bannerId && x.IDSanPham == sanPhamId);

            if (item != null)
            {
                _context.BannerSanPhams.Remove(item);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(GanSanPham), new { id = bannerId });
        }

        private string BuildBannerProductLink(int bannerId)
        {
            return Url.Action("SanPhamTheoLink", "Banner", new { area = "", id = bannerId }) ?? $"/Banner/SanPhamTheoLink/{bannerId}";
        }

        private async Task<List<BannerProductOptionVm>> LoadProductOptionsAsync()
        {
            return await _context.SanPhams
                .AsNoTracking()
                .OrderByDescending(sp => sp.TrangThai)
                .ThenBy(sp => sp.TenSanPham)
                .Select(sp => new BannerProductOptionVm
                {
                    ProductId = sp.IDSanPham,
                    ProductCode = sp.MaSanPham,
                    ProductName = sp.TenSanPham,
                    IsActive = sp.TrangThai
                })
                .ToListAsync();
        }

        private static System.Linq.Expressions.Expression<Func<SanPham, BannerProductItemVm>> ToBannerProductItemVm()
        {
            return sp => new BannerProductItemVm
            {
                ProductId = sp.IDSanPham,
                ProductCode = sp.MaSanPham,
                ProductName = sp.TenSanPham,
                IsActive = sp.TrangThai,
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
            };
        }
    }
}
