using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;

namespace QuanApi.Services
{
    public interface IVaiTroService
    {
        Task<List<VaiTro>> GetAllAsync(string? keyword);
        Task<VaiTro?> GetByIdAsync(Guid id);
        Task<bool> CreateAsync(VaiTro vt);
        Task<bool> UpdateAsync(Guid id, VaiTro vt);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ToggleStatusAsync(Guid id);
        Task<(int total, List<VaiTro> data)> GetPagedAsync(int page, int pageSize, string? keyword, string? trangThai);
    }
    public class VaiTroService : IVaiTroService
    {
        private readonly BanQuanAu1DbContext _context;

        public VaiTroService(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        public async Task<List<VaiTro>> GetAllAsync(string? keyword)
        {
            var query = _context.VaiTro.AsQueryable();
            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(x => x.TenVaiTro.Contains(keyword) || x.MaVaiTro.Contains(keyword));

            return await query.ToListAsync();
        }

        public async Task<VaiTro?> GetByIdAsync(Guid id)
        {
            return await _context.VaiTro.FindAsync(id);
        }

        public async Task<bool> CreateAsync(VaiTro vt)
        {
            vt.IDVaiTro = Guid.NewGuid();
            vt.NgayTao = DateTime.Now;
            if (string.IsNullOrEmpty(vt.NguoiTao)) vt.NguoiTao = "unknown";

            _context.VaiTro.Add(vt);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateAsync(Guid id, VaiTro vt)
        {
            var entity = await _context.VaiTro.FindAsync(id);
            if (entity == null) return false;

            entity.TenVaiTro = vt.TenVaiTro;
            entity.MaVaiTro = vt.MaVaiTro;
            entity.NgayTao = vt.NgayTao;
            entity.NguoiTao = vt.NguoiTao;
            entity.LanCapNhatCuoi = DateTime.Now;
            entity.NguoiCapNhat = string.IsNullOrEmpty(vt.NguoiCapNhat) ? "unknown" : vt.NguoiCapNhat;
            entity.TrangThai = vt.TrangThai;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _context.VaiTro.FindAsync(id);
            if (entity == null) return false;

            _context.VaiTro.Remove(entity);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> ToggleStatusAsync(Guid id)
        {
            var vt = await _context.VaiTro.FindAsync(id);
            if (vt == null) return false;

            vt.TrangThai = !vt.TrangThai;
            vt.LanCapNhatCuoi = DateTime.Now;
            vt.NguoiCapNhat = "auto-toggle";

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<(int total, List<VaiTro> data)> GetPagedAsync(int page, int pageSize, string? keyword, string? trangThai)
        {
            var query = _context.VaiTro.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(x => x.TenVaiTro.Contains(keyword) || x.MaVaiTro.Contains(keyword));

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
