using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;

namespace QuanApi.Services
{
    public interface IKieuDangService
    {
        Task<List<KieuDang>> GetAllAsync(string? keyword);
        Task<KieuDang?> GetByIdAsync(Guid id);
        Task<bool> CreateAsync(KieuDang kd);
        Task<bool> UpdateAsync(Guid id, KieuDang kd);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ToggleStatusAsync(Guid id);
        Task<(int total, List<KieuDang> data)> GetPagedAsync(int page, int pageSize, string? keyword, string? trangThai);
    }

    public class KieuDangService : IKieuDangService
    {
        private readonly BanQuanAu1DbContext _context;

        public KieuDangService(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        public async Task<List<KieuDang>> GetAllAsync(string? keyword)
        {
            var query = _context.KieuDangs.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(x =>
                    x.TenKieuDang.Contains(keyword) ||
                    x.MaKieuDang.Contains(keyword));

            return await query.ToListAsync();
        }

        public async Task<KieuDang?> GetByIdAsync(Guid id)
        {
            return await _context.KieuDangs.FindAsync(id);
        }

        public async Task<bool> CreateAsync(KieuDang kd)
        {
            kd.IDKieuDang = Guid.NewGuid();
            kd.NgayTao = DateTime.UtcNow;
            kd.NguoiTao ??= "unknown";

            _context.KieuDangs.Add(kd);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateAsync(Guid id, KieuDang kd)
        {
            var entity = await _context.KieuDangs.FindAsync(id);
            if (entity == null) return false;

            entity.TenKieuDang = kd.TenKieuDang;
            entity.MaKieuDang = kd.MaKieuDang;
            entity.TrangThai = kd.TrangThai;
            entity.LanCapNhatCuoi = DateTime.UtcNow;
            entity.NguoiCapNhat = string.IsNullOrEmpty(kd.NguoiCapNhat) ? "unknown" : kd.NguoiCapNhat;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _context.KieuDangs.FindAsync(id);
            if (entity == null) return false;

            _context.KieuDangs.Remove(entity);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> ToggleStatusAsync(Guid id)
        {
            var kd = await _context.KieuDangs.FindAsync(id);
            if (kd == null) return false;

            kd.TrangThai = !kd.TrangThai;
            kd.LanCapNhatCuoi = DateTime.UtcNow;
            kd.NguoiCapNhat = "auto-toggle";

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<(int total, List<KieuDang> data)> GetPagedAsync(
            int page, int pageSize, string? keyword, string? trangThai)
        {
            var query = _context.KieuDangs.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(x =>
                    x.TenKieuDang.Contains(keyword) ||
                    x.MaKieuDang.Contains(keyword));

            if (!string.IsNullOrEmpty(trangThai))
            {
                if (trangThai == "active") query = query.Where(x => x.TrangThai == true);
                else if (trangThai == "inactive") query = query.Where(x => x.TrangThai == false);
            }

            var total = await query.CountAsync();
            var data = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return (total, data);
        }
    }
}
