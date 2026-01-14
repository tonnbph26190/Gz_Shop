using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;

namespace QuanApi.Services
{
    public interface ILoaiOngService
    {
        Task<List<LoaiOng>> GetAllAsync(string? keyword);
        Task<LoaiOng?> GetByIdAsync(Guid id);
        Task<bool> CreateAsync(LoaiOng lo);
        Task<bool> UpdateAsync(Guid id, LoaiOng lo);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ToggleStatusAsync(Guid id);
        Task<(int total, List<LoaiOng> data)> GetPagedAsync(int page, int pageSize, string? keyword, string? trangThai);
    }

    public class LoaiOngService : ILoaiOngService
    {
        private readonly BanQuanAu1DbContext _context;

        public LoaiOngService(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        public async Task<List<LoaiOng>> GetAllAsync(string? keyword)
        {
            var query = _context.LoaiOngs.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(x =>
                    x.TenLoaiOng.Contains(keyword) ||
                    x.MaLoaiOng.Contains(keyword));

            return await query.ToListAsync();
        }

        public async Task<LoaiOng?> GetByIdAsync(Guid id)
        {
            return await _context.LoaiOngs.FindAsync(id);
        }

        public async Task<bool> CreateAsync(LoaiOng lo)
        {
            lo.IDLoaiOng = Guid.NewGuid();
            lo.NgayTao = DateTime.Now;
            lo.NguoiTao ??= "unknown";

            _context.LoaiOngs.Add(lo);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateAsync(Guid id, LoaiOng lo)
        {
            var entity = await _context.LoaiOngs.FindAsync(id);
            if (entity == null) return false;

            entity.TenLoaiOng = lo.TenLoaiOng;
            entity.MaLoaiOng = lo.MaLoaiOng;
            entity.TrangThai = lo.TrangThai;
            entity.LanCapNhatCuoi = DateTime.Now;
            entity.NguoiCapNhat = string.IsNullOrEmpty(lo.NguoiCapNhat) ? "unknown" : lo.NguoiCapNhat;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _context.LoaiOngs.FindAsync(id);
            if (entity == null) return false;

            _context.LoaiOngs.Remove(entity);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> ToggleStatusAsync(Guid id)
        {
            var lo = await _context.LoaiOngs.FindAsync(id);
            if (lo == null) return false;

            lo.TrangThai = !lo.TrangThai;
            lo.LanCapNhatCuoi = DateTime.Now;
            lo.NguoiCapNhat = "auto-toggle";

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<(int total, List<LoaiOng> data)> GetPagedAsync(
            int page, int pageSize, string? keyword, string? trangThai)
        {
            var query = _context.LoaiOngs.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(x =>
                    x.TenLoaiOng.Contains(keyword) ||
                    x.MaLoaiOng.Contains(keyword));

            if (!string.IsNullOrEmpty(trangThai))
            {
                if (trangThai == "active") query = query.Where(x => x.TrangThai == true);
                if (trangThai == "inactive") query = query.Where(x => x.TrangThai == false);
            }

            var total = await query.CountAsync();
            var data = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return (total, data);
        }
    }
}
