using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;

namespace QuanApi.Services
{
    // ================= INTERFACE =================
    public interface ISanPhamNguoiDungService
    {
        Task<List<SanPhamChiTiet>> GetAllAsync(string? keyword);
        Task<SanPhamChiTiet?> GetByIdAsync(Guid id);
        Task<(int total, List<SanPhamChiTiet> data)> GetPagedAsync(
            int page,
            int pageSize,
            string? keyword,
            decimal? giaTu,
            decimal? giaDen);

        Task<bool> ToggleStatusAsync(Guid id);
    }

    // ================= SERVICE =================
    public class SanPhamNguoiDungService : ISanPhamNguoiDungService
    {
        private readonly BanQuanAu1DbContext _context;

        public SanPhamNguoiDungService(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        // ---------- GET ALL ----------
        public async Task<List<SanPhamChiTiet>> GetAllAsync(string? keyword)
        {
            var query = _context.SanPhamChiTiets
                .Include(x => x.SanPham)
                .Include(x => x.KichCo)
                .Include(x => x.MauSac)
                .Include(x => x.SanPham.DanhMuc)
                .Where(x => x.TrangThai)
                .AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(x =>
                    x.SanPham.TenSanPham.Contains(keyword) ||
                    x.MaSPChiTiet.Contains(keyword));
            }

            return await query.ToListAsync();
        }

        // ---------- GET BY ID ----------
        public async Task<SanPhamChiTiet?> GetByIdAsync(Guid id)
        {
            return await _context.SanPhamChiTiets
                .Include(x => x.SanPham)
                .Include(x => x.KichCo)
                .Include(x => x.MauSac)
                .Include(x => x.SanPham.DanhMuc)
                .FirstOrDefaultAsync(x =>
                    x.IDSanPhamChiTiet == id &&
                    x.TrangThai);
        }

        // ---------- PAGING ----------
        public async Task<(int total, List<SanPhamChiTiet> data)> GetPagedAsync(
            int page,
            int pageSize,
            string? keyword,
            decimal? giaTu,
            decimal? giaDen)
        {
            var query = _context.SanPhamChiTiets
                .Include(x => x.SanPham)
                .Include(x => x.KichCo)
                .Include(x => x.MauSac)
                .Include(x => x.SanPham.DanhMuc)
                .Where(x => x.TrangThai)
                .AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(x =>
                    x.SanPham.TenSanPham.Contains(keyword) ||
                    x.MaSPChiTiet.Contains(keyword));
            }

            if (giaTu.HasValue)
                query = query.Where(x => x.GiaBan >= giaTu.Value);

            if (giaDen.HasValue)
                query = query.Where(x => x.GiaBan <= giaDen.Value);

            var total = await query.CountAsync();
            var data = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (total, data);
        }

        // ---------- TOGGLE STATUS ----------
        public async Task<bool> ToggleStatusAsync(Guid id)
        {
            var sp = await _context.SanPhamChiTiets.FindAsync(id);
            if (sp == null) return false;

            sp.TrangThai = !sp.TrangThai;
            sp.LanCapNhatCuoi = DateTime.Now;
            sp.NguoiCapNhat = "auto-toggle";

            return await _context.SaveChangesAsync() > 0;
        }
    }
}
