using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;

namespace QuanApi.Services
{
    public interface IDanhMucService
    {
        Task<List<DanhMuc>> GetAllAsync(string? keyword);
        Task<DanhMuc?> GetByIdAsync(Guid id);
        Task<bool> CreateAsync(DanhMuc dm);
        Task<bool> UpdateAsync(Guid id, DanhMuc dm);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ToggleStatusAsync(Guid id);
        Task<(int total, List<DanhMuc> data)> GetPagedAsync(int page, int pageSize, string? keyword, string? trangThai);
    }

    public class DanhMucService : IDanhMucService
    {
        private readonly BanQuanAu1DbContext _context;

        public DanhMucService(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        public async Task<List<DanhMuc>> GetAllAsync(string? keyword)
        {
            var query = _context.DanhMucs.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(x => x.TenDanhMuc.Contains(keyword) ||
                                         x.MaDanhMuc.Contains(keyword));

            return await query.ToListAsync();
        }

        public async Task<DanhMuc?> GetByIdAsync(Guid id)
        {
            return await _context.DanhMucs.FindAsync(id);
        }

        public async Task<bool> CreateAsync(DanhMuc dm)
        {
            dm.IDDanhMuc = Guid.NewGuid();
            dm.NgayTao = DateTime.Now;
            dm.NguoiTao ??= "unknown";

            _context.DanhMucs.Add(dm);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateAsync(Guid id, DanhMuc dm)
        {
            var entity = await _context.DanhMucs.FindAsync(id);
            if (entity == null) return false;

            entity.TenDanhMuc = dm.TenDanhMuc;
            entity.MaDanhMuc = dm.MaDanhMuc;
            entity.TrangThai = dm.TrangThai;

            entity.LanCapNhatCuoi = DateTime.Now;
            entity.NguoiCapNhat = string.IsNullOrEmpty(dm.NguoiCapNhat) ? "unknown" : dm.NguoiCapNhat;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _context.DanhMucs.FindAsync(id);
            if (entity == null) return false;

            _context.DanhMucs.Remove(entity);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> ToggleStatusAsync(Guid id)
        {
            var dm = await _context.DanhMucs.FindAsync(id);
            if (dm == null) return false;

            dm.TrangThai = !dm.TrangThai;
            dm.LanCapNhatCuoi = DateTime.Now;
            dm.NguoiCapNhat = "auto-toggle";

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<(int total, List<DanhMuc> data)> GetPagedAsync(int page, int pageSize, string? keyword, string? trangThai)
        {
            var query = _context.DanhMucs.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(x =>
                    x.TenDanhMuc.Contains(keyword) ||
                    x.MaDanhMuc.Contains(keyword));

            if (!string.IsNullOrEmpty(trangThai))
            {
                if (trangThai == "active") query = query.Where(x => x.TrangThai == true);
                if (trangThai == "inactive") query = query.Where(x => x.TrangThai == false);
            }

            var total = await query.CountAsync();
            var data = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (total, data);
        }
    }
}
