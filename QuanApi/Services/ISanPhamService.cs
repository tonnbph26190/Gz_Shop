using BanQuanAu1.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;
using QuanApi.Dtos;

namespace QuanApi.Services
{
    public interface ISanPhamService
    {
        Task<IEnumerable<SanPhamDto>> GetAllAsync();
        Task<(int total, List<SanPhamDto> data)> GetPagedAsync(
            int page,
            int pageSize,
            string? keyword,
            string? trangThai,
            decimal? priceFrom,
            decimal? priceTo,
            int? qtyFrom,
            int? qtyTo,
            DateTime? dateFrom,
            DateTime? dateTo);

        Task<SanPham?> GetByIdAsync(Guid id);
        Task<IActionResult> CreateAsync(SanPham sanPham, string userName);
        Task<IActionResult> UpdateAsync(Guid id, SanPham sanPham);
        Task<bool> DeleteAsync(Guid id);

        Task<AnhSanPham> AddProductImageAsync(
        Guid sanPhamChiTietId,
        AddAnhSanPhamDto dto,
        string nguoiThucHien
    );

        Task DeleteProductImageAsync(Guid imageId, string nguoiThucHien);

        Task SetMainImageAsync(Guid imageId, string nguoiThucHien);

        Task<List<AnhSanPhamDto>> GetProductImagesAsync(Guid sanPhamChiTietId);

        Task<AnhSanPham> UploadProductImageAsync(
            Guid sanPhamChiTietId,
            IFormFile file,
            bool laAnhChinh,
            string nguoiThucHien
        );
    }
    public class SanPhamService : ISanPhamService
    {
        private readonly BanQuanAu1DbContext _context;
        private readonly SanPhamValidationService _validationService;

        public SanPhamService(BanQuanAu1DbContext context)
        {
            _context = context;
            _validationService = new SanPhamValidationService();
        }

        #region GET ALL
        public async Task<IEnumerable<SanPhamDto>> GetAllAsync()
        {
            return await _context.SanPhams
                .IncludeAllSanPham()
                .SelectSanPhamDto()
                .ToListAsync();
        }
        #endregion

        #region PAGED
        public async Task<(int total, List<SanPhamDto> data)> GetPagedAsync(
            int page, int pageSize, string? keyword, string? trangThai,
            decimal? priceFrom, decimal? priceTo,
            int? qtyFrom, int? qtyTo,
            DateTime? dateFrom, DateTime? dateTo)
        {
            var query = _context.SanPhams.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(x => x.MaSanPham.Contains(keyword) || x.TenSanPham.Contains(keyword));

            if (trangThai == "active")
                query = query.Where(x => x.TrangThai);
            else if (trangThai == "inactive")
                query = query.Where(x => !x.TrangThai);

            if (dateFrom.HasValue)
                query = query.Where(x => x.NgayTao >= dateFrom.Value);

            if (dateTo.HasValue)
                query = query.Where(x => x.NgayTao <= dateTo.Value.Date.AddDays(1).AddTicks(-1));

            if (priceFrom.HasValue)
                query = query.Where(x => x.SanPhamChiTiets.Any(ct => ct.GiaBan >= priceFrom));

            if (priceTo.HasValue)
                query = query.Where(x => x.SanPhamChiTiets.Any(ct => ct.GiaBan <= priceTo));

            if (qtyFrom.HasValue)
                query = query.Where(x => x.SanPhamChiTiets.Any(ct => ct.SoLuong >= qtyFrom));

            if (qtyTo.HasValue)
                query = query.Where(x => x.SanPhamChiTiets.Any(ct => ct.SoLuong <= qtyTo));

            var total = await query.CountAsync();

            var data = await query
                .IncludeAllSanPham()
                .OrderBy(x => x.TenSanPham)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .SelectSanPhamDto()
                .ToListAsync();

            return (total, data);
        }
        #endregion

        #region CRUD
        public async Task<SanPham?> GetByIdAsync(Guid id)
            => await _context.SanPhams.FindAsync(id);

        public async Task<IActionResult> CreateAsync(SanPham sanPham, string userName)
        {
            var errors = _validationService.ValidateSanPham(sanPham);
            if (errors.Any())
                return new BadRequestObjectResult(new { errors });

            var chiTiets = sanPham.SanPhamChiTiets?.ToList();
            sanPham.SanPhamChiTiets = null;
            sanPham.IDSanPham = Guid.NewGuid();

            _context.SanPhams.Add(sanPham);
            await _context.SaveChangesAsync();

            if (chiTiets != null && chiTiets.Any())
            {
                foreach (var ct in chiTiets)
                {
                    ct.IDSanPhamChiTiet = Guid.NewGuid();
                    ct.IDSanPham = sanPham.IDSanPham;
                }

                await _context.SanPhamChiTiets.AddRangeAsync(chiTiets);
                await _context.SaveChangesAsync();
            }

            return new CreatedAtActionResult(nameof(GetByIdAsync), "SanPhams",
                new { id = sanPham.IDSanPham }, sanPham);
        }

        public async Task<IActionResult> UpdateAsync(Guid id, SanPham sanPham)
        {
            if (id != sanPham.IDSanPham)
                return new BadRequestResult();

            _context.Entry(sanPham).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return new NoContentResult();
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var sp = await _context.SanPhams.FindAsync(id);
            if (sp == null) return false;

            _context.SanPhams.Remove(sp);
            await _context.SaveChangesAsync();
            return true;
        }
        #endregion

        #region IMAGE
        public async Task<AnhSanPham> AddProductImageAsync(
        Guid sanPhamChiTietId,
        AddAnhSanPhamDto dto,
        string nguoiThucHien)
        {
            var sanPhamChiTiet = await _context.SanPhamChiTiets.FindAsync(sanPhamChiTietId);
            if (sanPhamChiTiet == null)
                throw new KeyNotFoundException("Không tìm thấy sản phẩm chi tiết.");

            if (dto.LaAnhChinh)
            {
                var anhChinhCu = await _context.AnhSanPhams
                    .FirstOrDefaultAsync(a =>
                        a.IDSanPhamChiTiet == sanPhamChiTietId &&
                        a.LaAnhChinh &&
                        a.TrangThai);

                if (anhChinhCu != null)
                {
                    anhChinhCu.LaAnhChinh = false;
                    anhChinhCu.LanCapNhatCuoi = DateTime.UtcNow;
                    anhChinhCu.NguoiCapNhat = nguoiThucHien;
                }
            }

            var anhSanPham = new AnhSanPham
            {
                IDAnhSanPham = Guid.NewGuid(),
                MaAnh = $"IMG_{DateTime.Now:yyyyMMddHHmmssfff}",
                IDSanPhamChiTiet = sanPhamChiTietId,
                UrlAnh = dto.UrlAnh,
                LaAnhChinh = dto.LaAnhChinh,
                NgayTao = DateTime.UtcNow,
                NguoiTao = nguoiThucHien,
                TrangThai = true
            };

            _context.AnhSanPhams.Add(anhSanPham);
            await _context.SaveChangesAsync();

            return anhSanPham;
        }

        public async Task DeleteProductImageAsync(Guid imageId, string nguoiThucHien)
        {
            var anhSanPham = await _context.AnhSanPhams.FindAsync(imageId);
            if (anhSanPham == null)
                throw new KeyNotFoundException("Không tìm thấy ảnh sản phẩm.");

            anhSanPham.TrangThai = false;
            anhSanPham.LanCapNhatCuoi = DateTime.UtcNow;
            anhSanPham.NguoiCapNhat = nguoiThucHien;

            await _context.SaveChangesAsync();
        }

        public async Task SetMainImageAsync(Guid imageId, string nguoiThucHien)
        {
            var anhSanPham = await _context.AnhSanPhams.FindAsync(imageId);
            if (anhSanPham == null)
                throw new KeyNotFoundException("Không tìm thấy ảnh sản phẩm.");

            if (!anhSanPham.TrangThai)
                throw new InvalidOperationException("Ảnh sản phẩm đã bị vô hiệu hóa.");

            var anhChinhCu = await _context.AnhSanPhams
                .FirstOrDefaultAsync(a =>
                    a.IDSanPhamChiTiet == anhSanPham.IDSanPhamChiTiet &&
                    a.LaAnhChinh &&
                    a.TrangThai &&
                    a.IDAnhSanPham != imageId);

            if (anhChinhCu != null)
            {
                anhChinhCu.LaAnhChinh = false;
                anhChinhCu.LanCapNhatCuoi = DateTime.UtcNow;
                anhChinhCu.NguoiCapNhat = nguoiThucHien;
            }

            anhSanPham.LaAnhChinh = true;
            anhSanPham.LanCapNhatCuoi = DateTime.UtcNow;
            anhSanPham.NguoiCapNhat = nguoiThucHien;

            await _context.SaveChangesAsync();
        }

        public async Task<List<AnhSanPhamDto>> GetProductImagesAsync(Guid sanPhamChiTietId)
        {
            var sanPhamChiTiet = await _context.SanPhamChiTiets.FindAsync(sanPhamChiTietId);
            if (sanPhamChiTiet == null)
                throw new KeyNotFoundException("Không tìm thấy sản phẩm chi tiết.");

            return await _context.AnhSanPhams
                .Where(a => a.IDSanPhamChiTiet == sanPhamChiTietId && a.TrangThai)
                .OrderByDescending(a => a.LaAnhChinh)
                .ThenBy(a => a.NgayTao)
                .Select(a => new AnhSanPhamDto
                {
                    IDAnhSanPham = a.IDAnhSanPham,
                    MaAnh = a.MaAnh,
                    IDSanPhamChiTiet = a.IDSanPhamChiTiet,
                    UrlAnh = a.UrlAnh,
                    LaAnhChinh = a.LaAnhChinh,
                    NgayTao = a.NgayTao,
                    NguoiTao = a.NguoiTao,
                    LanCapNhatCuoi = a.LanCapNhatCuoi,
                    NguoiCapNhat = a.NguoiCapNhat,
                    TrangThai = a.TrangThai
                })
                .ToListAsync();
        }

        public async Task<AnhSanPham> UploadProductImageAsync(
            Guid sanPhamChiTietId,
            IFormFile file,
            bool laAnhChinh,
            string nguoiThucHien)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Không có file ảnh.");

            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var uploadPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot", "uploads", fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(uploadPath)!);

            using (var stream = new FileStream(uploadPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var dto = new AddAnhSanPhamDto
            {
                UrlAnh = $"/uploads/{fileName}",
                LaAnhChinh = laAnhChinh
            };

            return await AddProductImageAsync(sanPhamChiTietId, dto, nguoiThucHien);
        }
        #endregion
    }
    public static class SanPhamQueryExtensions
    {
        public static IQueryable<SanPham> IncludeAllSanPham(this IQueryable<SanPham> query)
        {
            return query
                .Include(s => s.ChatLieu)
                .Include(s => s.DanhMuc)
                .Include(s => s.ThuongHieu)
                .Include(s => s.LoaiOng)
                .Include(s => s.KieuDang)
                .Include(s => s.LungQuan)

                .Include(s => s.SanPhamChiTiets)
                    .ThenInclude(ct => ct.KichCo)

                .Include(s => s.SanPhamChiTiets)
                    .ThenInclude(ct => ct.MauSac)

                .Include(s => s.SanPhamChiTiets)
                    .ThenInclude(ct => ct.HoaTiet)

                .Include(s => s.SanPhamChiTiets)
                    .ThenInclude(ct => ct.AnhSanPhams
                        .Where(a => a.TrangThai));
        }
        public static IQueryable<SanPhamDto> SelectSanPhamDto(this IQueryable<SanPham> query)
        {
            return query.Select(s => new SanPhamDto
            {
                IDSanPham = s.IDSanPham,
                MaSanPham = s.MaSanPham,
                TenSanPham = s.TenSanPham,

                IDChatLieu = s.IDChatLieu,
                IDDanhMuc = s.IDDanhMuc,
                IDThuongHieu = s.IDThuongHieu,
                IDLoaiOng = s.IDLoaiOng,
                IDKieuDang = s.IDKieuDang,
                IDLungQuan = s.IDLungQuan,

                CoXepLy = s.CoXepLy,
                CoGian = s.CoGian,
                TrangThai = s.TrangThai,

                TenChatLieu = s.ChatLieu.TenChatLieu,
                TenDanhMuc = s.DanhMuc.TenDanhMuc,
                TenThuongHieu = s.ThuongHieu.TenThuongHieu,
                TenLoaiOng = s.LoaiOng.TenLoaiOng,
                TenKieuDang = s.KieuDang.TenKieuDang,
                TenLungQuan = s.LungQuan.TenLungQuan,

                // Ảnh chính
                AnhChinh = s.SanPhamChiTiets
                    .Where(ct => ct.AnhSanPhams != null && ct.AnhSanPhams.Any())
                    .SelectMany(ct => ct.AnhSanPhams)
                    .Where(a => a.TrangThai)
                    .OrderByDescending(a => a.LaAnhChinh)
                    .ThenBy(a => a.NgayTao)
                    .Select(a => a.UrlAnh)
                    .FirstOrDefault(),

                // Danh sách ảnh
                DanhSachAnh = s.SanPhamChiTiets
                    .Where(ct => ct.AnhSanPhams != null && ct.AnhSanPhams.Any())
                    .SelectMany(ct => ct.AnhSanPhams)
                    .Where(a => a.TrangThai)
                    .OrderByDescending(a => a.LaAnhChinh)
                    .ThenBy(a => a.NgayTao)
                    .Select(a => new AnhSanPhamDto
                    {
                        IDAnhSanPham = a.IDAnhSanPham,
                        MaAnh = a.MaAnh,
                        IDSanPhamChiTiet = a.IDSanPhamChiTiet,
                        UrlAnh = a.UrlAnh,
                        LaAnhChinh = a.LaAnhChinh,
                        NgayTao = a.NgayTao,
                        NguoiTao = a.NguoiTao,
                        LanCapNhatCuoi = a.LanCapNhatCuoi,
                        NguoiCapNhat = a.NguoiCapNhat,
                        TrangThai = a.TrangThai
                    }).ToList(),

                // Chi tiết sản phẩm
                ChiTietSanPhams = s.SanPhamChiTiets.Select(ct => new SanPhamChiTietDto
                {
                    IdSanPhamChiTiet = ct.IDSanPhamChiTiet,
                    IdSanPham = ct.IDSanPham,
                    IdKichCo = ct.IDKichCo,
                    IdMauSac = ct.IDMauSac,
                    IdHoaTiet = ct.IDHoaTiet ?? Guid.Empty,

                    SoLuong = ct.SoLuong,
                    GiaBan = ct.GiaBan,
                    price = ct.GiaBan,
                    originalPrice = ct.GiaBan,

                    TenKichCo = ct.KichCo.TenKichCo,
                    TenMauSac = ct.MauSac.TenMauSac,
                    TenHoaTiet = ct.HoaTiet != null ? ct.HoaTiet.TenHoaTiet : null,

                    AnhDaiDien =
                        ct.AnhSanPhams != null && ct.AnhSanPhams.Any()
                            ? ct.AnhSanPhams
                                .Where(a => a.TrangThai && a.LaAnhChinh)
                                .Select(a => a.UrlAnh)
                                .FirstOrDefault()
                                ?? ct.AnhSanPhams
                                    .Where(a => a.TrangThai)
                                    .Select(a => a.UrlAnh)
                                    .FirstOrDefault()
                                ?? ""
                            : ""
                }).ToList()
            });
        }
    }
}
