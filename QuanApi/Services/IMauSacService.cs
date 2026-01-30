using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;

namespace QuanApi.Services
{
    public interface IMauSacService
    {
        Task<List<MauSac>> GetAllAsync(string? keyword);
        Task<MauSac?> GetByIdAsync(Guid id);
        Task<bool> CreateAsync(MauSac ms);
        Task<bool> UpdateAsync(Guid id, MauSac ms);
        Task<bool> DeleteAsync(Guid id);
        Task<(bool Success, bool NewStatus)> ToggleStatusAsync(Guid id);
        Task<(int Total, List<MauSac> Data)> GetPagedAsync(int page, int pageSize, string? keyword, string? trangThai);
    }
    public class MauSacService : IMauSacService
    {
        private readonly BanQuanAu1DbContext _context;

        public MauSacService(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        public async Task<List<MauSac>> GetAllAsync(string? keyword)
        {
            var query = _context.MauSacs.AsQueryable();
            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(x => x.TenMauSac.Contains(keyword) || x.MaMauSac.Contains(keyword));

            return await query.ToListAsync();
        }

        public async Task<MauSac?> GetByIdAsync(Guid id) => await _context.MauSacs.FindAsync(id);

        public async Task<bool> CreateAsync(MauSac ms)
        {
            ms.IDMauSac = Guid.NewGuid();
            ms.NgayTao = DateTime.UtcNow;
            if (string.IsNullOrEmpty(ms.NguoiTao)) ms.NguoiTao = "unknown";

            _context.MauSacs.Add(ms);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateAsync(Guid id, MauSac ms)
        {
            var entity = await _context.MauSacs.FindAsync(id);
            if (entity == null) return false;

            entity.TenMauSac = ms.TenMauSac;
            entity.MaMauSac = ms.MaMauSac;
            entity.LanCapNhatCuoi = DateTime.UtcNow;
            entity.NguoiCapNhat = string.IsNullOrEmpty(ms.NguoiCapNhat) ? "unknown" : ms.NguoiCapNhat;
            entity.TrangThai = ms.TrangThai;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _context.MauSacs.FindAsync(id);
            if (entity == null) return false;

            _context.MauSacs.Remove(entity);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<(bool Success, bool NewStatus)> ToggleStatusAsync(Guid id)
        {
            var ms = await _context.MauSacs.FindAsync(id);
            if (ms == null) return (false, false);

            ms.TrangThai = !ms.TrangThai;
            ms.LanCapNhatCuoi = DateTime.UtcNow;
            ms.NguoiCapNhat = "auto-toggle";

            var saved = await _context.SaveChangesAsync() > 0;
            return (saved, ms.TrangThai);
        }

        public async Task<(int Total, List<MauSac> Data)> GetPagedAsync(int page, int pageSize, string? keyword, string? trangThai)
        {
            var query = _context.MauSacs.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(x => x.TenMauSac.Contains(keyword) || x.MaMauSac.Contains(keyword));

            if (trangThai == "active") query = query.Where(x => x.TrangThai == true);
            else if (trangThai == "inactive") query = query.Where(x => x.TrangThai == false);

            var total = await query.CountAsync();
            var data = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return (total, data);
        }
    }
}
