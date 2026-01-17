using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;

namespace QuanApi.Services
{
    public interface IKhachHangService
    {
        Task<List<KhachHang>> GetAllAsync(string? keyword);
        Task<KhachHang?> GetByIdAsync(Guid id);
        Task<bool> CreateAsync(KhachHang kh);
        Task<bool> UpdateAsync(Guid id, KhachHang kh);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ToggleStatusAsync(Guid id);
        Task<(int total, List<KhachHang> data)> GetPagedAsync(
            int page,
            int pageSize,
            string? keyword,
            string? trangThai);
    }

    public class KhachHangService : IKhachHangService
    {
        private readonly BanQuanAu1DbContext _context;

        public KhachHangService(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        public async Task<List<KhachHang>> GetAllAsync(string? keyword)
        {
            var query = _context.KhachHang.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(x =>
                    x.TenKhachHang.Contains(keyword) ||
                    x.MaKhachHang.Contains(keyword));

            return await query.ToListAsync();
        }

        public async Task<KhachHang?> GetByIdAsync(Guid id)
        {
            return await _context.KhachHang.FindAsync(id);
        }

        public async Task<bool> CreateAsync(KhachHang kh)
        {
            kh.IDKhachHang = Guid.NewGuid();
            kh.NgayTao = DateTime.Now;
            if (string.IsNullOrEmpty(kh.NguoiTao))
                kh.NguoiTao = "unknown";

            _context.KhachHang.Add(kh);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateAsync(Guid id, KhachHang kh)
        {
            var entity = await _context.KhachHang.FindAsync(id);
            if (entity == null) return false;

            entity.TenKhachHang = kh.TenKhachHang;
            entity.MaKhachHang = kh.MaKhachHang;
            entity.TrangThai = kh.TrangThai;
            entity.LanCapNhatCuoi = DateTime.Now;
            entity.NguoiCapNhat =
                string.IsNullOrEmpty(kh.NguoiCapNhat) ? "unknown" : kh.NguoiCapNhat;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _context.KhachHang.FindAsync(id);
            if (entity == null) return false;

            _context.KhachHang.Remove(entity);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> ToggleStatusAsync(Guid id)
        {
            var kh = await _context.KhachHang.FindAsync(id);
            if (kh == null) return false;

            kh.TrangThai = !kh.TrangThai;
            kh.LanCapNhatCuoi = DateTime.Now;
            kh.NguoiCapNhat = "auto-toggle";

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<(int total, List<KhachHang> data)> GetPagedAsync(
            int page,
            int pageSize,
            string? keyword,
            string? trangThai)
        {
            var query = _context.KhachHang.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(x =>
                    x.TenKhachHang.Contains(keyword) ||
                    x.MaKhachHang.Contains(keyword));

            if (!string.IsNullOrEmpty(trangThai))
            {
                if (trangThai == "active")
                    query = query.Where(x => x.TrangThai == true);
                else if (trangThai == "inactive")
                    query = query.Where(x => x.TrangThai == false);
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
