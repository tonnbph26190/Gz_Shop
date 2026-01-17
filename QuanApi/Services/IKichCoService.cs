using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;

namespace QuanApi.Services
{
    public interface IKichCoService
    {
        Task<List<KichCo>> GetAllAsync(string? keyword);
        Task<KichCo?> GetByIdAsync(Guid id);
        Task<bool> CreateAsync(KichCo kc);
        Task<bool> UpdateAsync(Guid id, KichCo kc);
        Task<bool> DeleteAsync(Guid id);
        Task<(bool Success, bool NewStatus)> ToggleStatusAsync(Guid id);
        Task<(int Total, List<KichCo> Data)> GetPagedAsync(int page, int pageSize, string? keyword, string? trangThai);
    }
    public class KichCoService : IKichCoService
    {
        private readonly BanQuanAu1DbContext _context;

        public KichCoService(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        public async Task<List<KichCo>> GetAllAsync(string? keyword)
        {
            var query = _context.KichCos.AsQueryable();
            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(x => x.TenKichCo.Contains(keyword) || x.MaKichCo.Contains(keyword));

            return await query.ToListAsync();
        }

        public async Task<KichCo?> GetByIdAsync(Guid id) => await _context.KichCos.FindAsync(id);

        public async Task<bool> CreateAsync(KichCo kc)
        {
            kc.IDKichCo = Guid.NewGuid();
            kc.NgayTao = DateTime.Now;
            if (string.IsNullOrEmpty(kc.NguoiTao)) kc.NguoiTao = "unknown";

            _context.KichCos.Add(kc);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateAsync(Guid id, KichCo kc)
        {
            var entity = await _context.KichCos.FindAsync(id);
            if (entity == null) return false;

            entity.TenKichCo = kc.TenKichCo;
            entity.MaKichCo = kc.MaKichCo;
            entity.LanCapNhatCuoi = DateTime.Now;
            entity.NguoiCapNhat = string.IsNullOrEmpty(kc.NguoiCapNhat) ? "unknown" : kc.NguoiCapNhat;
            entity.TrangThai = kc.TrangThai;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _context.KichCos.FindAsync(id);
            if (entity == null) return false;

            _context.KichCos.Remove(entity);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<(bool Success, bool NewStatus)> ToggleStatusAsync(Guid id)
        {
            var kc = await _context.KichCos.FindAsync(id);
            if (kc == null) return (false, false);

            kc.TrangThai = !kc.TrangThai;
            kc.LanCapNhatCuoi = DateTime.Now;
            kc.NguoiCapNhat = "auto-toggle";

            var saved = await _context.SaveChangesAsync() > 0;
            return (saved, kc.TrangThai);
        }

        public async Task<(int Total, List<KichCo> Data)> GetPagedAsync(int page, int pageSize, string? keyword, string? trangThai)
        {
            var query = _context.KichCos.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(x => x.TenKichCo.Contains(keyword) || x.MaKichCo.Contains(keyword));

            if (trangThai == "active") query = query.Where(x => x.TrangThai == true);
            else if (trangThai == "inactive") query = query.Where(x => x.TrangThai == false);

            var total = await query.CountAsync();
            var data = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return (total, data);
        }
    }
}
