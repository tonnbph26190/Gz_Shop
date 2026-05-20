using BanQuanAu1.Web.Data;
using QuanApi.Data;
using System.Linq.Expressions;
using QuanApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace QuanApi.Services
{
    public interface ISanPhamChiTietService
    {
        Task<List<SanPhamChiTietDto>> GetAllAsync();
        Task<SanPhamChiTietDto?> GetByIdAsync(Guid id);
        Task<List<SanPhamChiTietDto>> GetBySanPhamAsync(Guid sanPhamId);

        Task<(bool merged, Guid id, int totalQuantity)> CreateAsync(SanPhamChiTietDto dto);
        Task UpdateAsync(Guid id, SanPhamChiTietDto dto);
        Task DeleteAsync(Guid id);
        Task BulkUpdateAsync(List<SanPhamChiTietDto> dtos);
    }
    public class SanPhamChiTietService : ISanPhamChiTietService
    {
        private readonly BanQuanAu1DbContext _context;
        private readonly ILogger<SanPhamChiTietService> _logger;

        public SanPhamChiTietService(
            BanQuanAu1DbContext context,
            ILogger<SanPhamChiTietService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<SanPhamChiTietDto>> GetAllAsync()
        {
            return await BuildBaseQuery()
                .Select(MapToDto())
                .ToListAsync();
        }

        public async Task<SanPhamChiTietDto?> GetByIdAsync(Guid id)
        {
            return await BuildBaseQuery()
                .Where(ct => ct.IDSanPhamChiTiet == id)
                .Select(MapToDto())
                .FirstOrDefaultAsync();
        }

        public async Task<List<SanPhamChiTietDto>> GetBySanPhamAsync(Guid sanPhamId)
        {
            var list = await _context.SanPhamChiTiets
                .Where(ct => ct.IDSanPham == sanPhamId)
                .IncludeAll()
                .ToListAsync();

            return list.Select(MapToDtoCompiled()).ToList();
        }

        public async Task<(bool merged, Guid id, int totalQuantity)> CreateAsync(SanPhamChiTietDto dto)
        {
            var existing = await _context.SanPhamChiTiets.FirstOrDefaultAsync(ct =>
                ct.IDSanPham == dto.IdSanPham &&
                ct.IDKichCo == dto.IdKichCo &&
                ct.IDMauSac == dto.IdMauSac &&
                ct.IDHoaTiet == (dto.IdHoaTiet == Guid.Empty ? null : dto.IdHoaTiet));

            if (existing != null)
            {
                existing.SoLuong += dto.SoLuong;
                existing.GiaBan = dto.GiaBan;
                await _context.SaveChangesAsync();

                return (true, existing.IDSanPhamChiTiet, existing.SoLuong);
            }

            var entity = new SanPhamChiTiet
            {
                IDSanPhamChiTiet = dto.IdSanPhamChiTiet == Guid.Empty ? Guid.NewGuid() : dto.IdSanPhamChiTiet,
                IDSanPham = dto.IdSanPham,
                IDKichCo = dto.IdKichCo,
                IDMauSac = dto.IdMauSac,
                IDHoaTiet = dto.IdHoaTiet == Guid.Empty ? null : dto.IdHoaTiet,
                SoLuong = dto.SoLuong,
                GiaBan = dto.GiaBan,
                MaSPChiTiet = dto.MaSPChiTiet ?? $"CT_{DateTime.UtcNow.Ticks.ToString()[^6..]}"
            };

            _context.SanPhamChiTiets.Add(entity);
            await _context.SaveChangesAsync();

            return (false, entity.IDSanPhamChiTiet, entity.SoLuong);
        }

        public async Task UpdateAsync(Guid id, SanPhamChiTietDto dto)
        {
            var entity = await _context.SanPhamChiTiets.FindAsync(id);
            if (entity == null)
                throw new KeyNotFoundException();

            entity.IDSanPham = dto.IdSanPham;
            entity.IDKichCo = dto.IdKichCo;
            entity.IDMauSac = dto.IdMauSac;
            entity.IDHoaTiet = dto.IdHoaTiet == Guid.Empty ? null : dto.IdHoaTiet;
            entity.SoLuong = dto.SoLuong;
            entity.GiaBan = dto.GiaBan;
            entity.MaSPChiTiet = string.IsNullOrEmpty(dto.MaSPChiTiet)
                ? entity.MaSPChiTiet
                : dto.MaSPChiTiet;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var ct = await _context.SanPhamChiTiets.FindAsync(id);
            if (ct == null)
                throw new KeyNotFoundException();

            _context.SanPhamChiTiets.Remove(ct);
            await _context.SaveChangesAsync();
        }

        public async Task BulkUpdateAsync(List<SanPhamChiTietDto> dtos)
        {
            foreach (var dto in dtos)
            {
                var ct = await _context.SanPhamChiTiets.FindAsync(dto.IdSanPhamChiTiet);
                if (ct == null) continue;

                ct.SoLuong = dto.SoLuong;
                ct.GiaBan = dto.GiaBan;
            }

            await _context.SaveChangesAsync();
        }

        // =========================
        // 🔧 HELPER
        // =========================

        private IQueryable<SanPhamChiTiet> BuildBaseQuery()
            => _context.SanPhamChiTiets.IncludeAll();

        private static Expression<Func<SanPhamChiTiet, SanPhamChiTietDto>> MapToDto()
        {
            return ct => new SanPhamChiTietDto
            {
                IdSanPhamChiTiet = ct.IDSanPhamChiTiet,
                IdSanPham = ct.IDSanPham,
                IdKichCo = ct.IDKichCo,
                IdMauSac = ct.IDMauSac,
                IdHoaTiet = ct.IDHoaTiet ?? Guid.Empty,
                SoLuong = ct.SoLuong,
                SoLuongVatLy = ct.SoLuong,
                SoLuongDatCho = ct.SoLuongDatCho,
                SoLuongKhaDung = Math.Max(0, ct.SoLuong - ct.SoLuongDatCho),
                GiaBan = ct.GiaBan,
                MaSPChiTiet = ct.MaSPChiTiet,
                TenKichCo = ct.KichCo.TenKichCo,
                TenMauSac = ct.MauSac.TenMauSac,
                TenHoaTiet = ct.HoaTiet != null ? ct.HoaTiet.TenHoaTiet : "N/A",
                TenSanPham = ct.SanPham.TenSanPham,
                TrangThai = ct.SanPham.TrangThai,
                TenDanhMuc = ct.SanPham.DanhMuc.TenDanhMuc,
                AnhDaiDien = ct.AnhSanPhams
                    .Where(a => a.LaAnhChinh && a.TrangThai)
                    .Select(a => a.UrlAnh)
                    .FirstOrDefault() ?? ""
            };
        }

        private static Func<SanPhamChiTiet, SanPhamChiTietDto> MapToDtoCompiled()
            => MapToDto().Compile();
    }

    public static class SanPhamChiTietIncludeExtensions
    {
        public static IQueryable<SanPhamChiTiet> IncludeAll(this IQueryable<SanPhamChiTiet> query)
        {
            return query
                .Include(ct => ct.KichCo)
                .Include(ct => ct.MauSac)
                .Include(ct => ct.HoaTiet)
                .Include(ct => ct.SanPham).ThenInclude(s => s.DanhMuc)
                .Include(ct => ct.AnhSanPhams.Where(a => a.TrangThai));
        }
    }

}
