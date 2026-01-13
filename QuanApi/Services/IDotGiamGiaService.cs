using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;

namespace QuanApi.Services
{
    public interface IDotGiamGiaService
    {
        Task<List<DotGiamGia>> GetAllAsync(string? keyword);
        Task<DotGiamGia?> GetByIdAsync(Guid id);
        Task<bool> CreateAsync(DotGiamGia dot, List<Guid> sanPhamChiTietIds);
        Task<bool> UpdateAsync(Guid id, DotGiamGia dot, List<Guid> sanPhamChiTietIds);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ToggleStatusAsync(Guid id);
        Task<(int total, List<DotGiamGia> data)> GetPagedAsync(
            int page, int pageSize, string? keyword, string? trangThai);
    }

    public class DotGiamGiaService : IDotGiamGiaService
    {
        private readonly BanQuanAu1DbContext _context;

        public DotGiamGiaService(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        // ================= GET ALL =================
        public async Task<List<DotGiamGia>> GetAllAsync(string? keyword)
        {
            var query = _context.DotGiamGias.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(x =>
                    x.MaDot.Contains(keyword) ||
                    x.TenDot.Contains(keyword));

            return await query
                .Include(x => x.SanPhamDotGiams)
                .ToListAsync();
        }

        // ================= GET BY ID =================
        public async Task<DotGiamGia?> GetByIdAsync(Guid id)
        {
            return await _context.DotGiamGias
                .Include(x => x.SanPhamDotGiams)
                .FirstOrDefaultAsync(x => x.IDDotGiamGia == id);
        }

        // ================= CREATE =================
        public async Task<bool> CreateAsync(DotGiamGia dot, List<Guid> sanPhamChiTietIds)
        {
            dot.IDDotGiamGia = Guid.NewGuid();
            dot.NgayTao = DateTime.Now;
            dot.TrangThai = dot.NgayBatDau <= DateTime.Now && dot.NgayKetThuc >= DateTime.Now;

            _context.DotGiamGias.Add(dot);

            foreach (var spctId in sanPhamChiTietIds)
            {
                _context.SanPhamDotGiams.Add(new SanPhamDotGiam
                {
                    IDSanPhamDotGiam = Guid.NewGuid(),
                    IDDotGiamGia = dot.IDDotGiamGia,
                    IDSanPhamChiTiet = spctId
                });
            }

            return await _context.SaveChangesAsync() > 0;
        }

        // ================= UPDATE =================
        public async Task<bool> UpdateAsync(Guid id, DotGiamGia dot, List<Guid> sanPhamChiTietIds)
        {
            var entity = await _context.DotGiamGias
                .Include(x => x.SanPhamDotGiams)
                .FirstOrDefaultAsync(x => x.IDDotGiamGia == id);

            if (entity == null) return false;

            entity.MaDot = dot.MaDot;
            entity.TenDot = dot.TenDot;
            entity.PhanTramGiam = dot.PhanTramGiam;
            entity.NgayBatDau = dot.NgayBatDau;
            entity.NgayKetThuc = dot.NgayKetThuc;
            entity.LanCapNhatCuoi = DateTime.Now;
            entity.NguoiCapNhat = string.IsNullOrEmpty(dot.NguoiCapNhat) ? "unknown" : dot.NguoiCapNhat;
            entity.TrangThai = dot.NgayBatDau <= DateTime.Now && dot.NgayKetThuc >= DateTime.Now;

            // Xóa liên kết cũ
            _context.SanPhamDotGiams.RemoveRange(entity.SanPhamDotGiams);

            // Thêm lại liên kết mới
            foreach (var spctId in sanPhamChiTietIds)
            {
                _context.SanPhamDotGiams.Add(new SanPhamDotGiam
                {
                    IDSanPhamDotGiam = Guid.NewGuid(),
                    IDDotGiamGia = id,
                    IDSanPhamChiTiet = spctId
                });
            }

            return await _context.SaveChangesAsync() > 0;
        }

        // ================= DELETE =================
        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _context.DotGiamGias.FindAsync(id);
            if (entity == null) return false;

            var links = _context.SanPhamDotGiams
                .Where(x => x.IDDotGiamGia == id);

            _context.SanPhamDotGiams.RemoveRange(links);
            _context.DotGiamGias.Remove(entity);

            return await _context.SaveChangesAsync() > 0;
        }

        // ================= TOGGLE STATUS =================
        public async Task<bool> ToggleStatusAsync(Guid id)
        {
            var dot = await _context.DotGiamGias.FindAsync(id);
            if (dot == null) return false;

            dot.TrangThai = !dot.TrangThai;
            dot.LanCapNhatCuoi = DateTime.Now;
            dot.NguoiCapNhat = "auto-toggle";

            return await _context.SaveChangesAsync() > 0;
        }

        // ================= PAGED =================
        public async Task<(int total, List<DotGiamGia> data)> GetPagedAsync(
            int page, int pageSize, string? keyword, string? trangThai)
        {
            var query = _context.DotGiamGias.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(x =>
                    x.MaDot.Contains(keyword) ||
                    x.TenDot.Contains(keyword));

            if (!string.IsNullOrEmpty(trangThai))
            {
                if (trangThai == "active") query = query.Where(x => x.TrangThai);
                else if (trangThai == "inactive") query = query.Where(x => !x.TrangThai);
            }

            var total = await query.CountAsync();
            var data = await query
                .OrderByDescending(x => x.NgayTao)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (total, data);
        }
    }
}
