using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;

namespace QuanApi.Services
{
    public interface IHoaTietService
    {
        Task<IEnumerable<HoaTiet>> GetAllAsync(string? keyword);
        Task<HoaTiet?> GetByIdAsync(Guid id);
        Task<bool> CreateAsync(HoaTiet ht);
        Task<bool> UpdateAsync(Guid id, HoaTiet ht);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ToggleStatusAsync(Guid id);
        Task<(int total, List<HoaTiet> data)> GetPagedAsync(int page, int pageSize, string? keyword, string? trangThai);
    }
   
        public class HoaTietService : IHoaTietService
        {
            private readonly BanQuanAu1DbContext _context;

            public HoaTietService(BanQuanAu1DbContext context)
            {
                _context = context;
            }

            public async Task<IEnumerable<HoaTiet>> GetAllAsync(string? keyword)
            {
                var query = _context.HoaTiet.AsQueryable();

                if (!string.IsNullOrEmpty(keyword))
                    query = query.Where(x => x.TenHoaTiet.Contains(keyword) || x.MaHoaTiet.Contains(keyword));

                return await query.ToListAsync();
            }

            public async Task<HoaTiet?> GetByIdAsync(Guid id)
            {
                return await _context.HoaTiet.FindAsync(id);
            }

            public async Task<bool> CreateAsync(HoaTiet ht)
            {
                ht.IDHoaTiet = Guid.NewGuid();
                ht.NgayTao = DateTime.UtcNow;
                ht.NguoiTao = string.IsNullOrEmpty(ht.NguoiTao) ? "unknown" : ht.NguoiTao;

                _context.HoaTiet.Add(ht);
                return await _context.SaveChangesAsync() > 0;
            }

            public async Task<bool> UpdateAsync(Guid id, HoaTiet ht)
            {
                var entity = await _context.HoaTiet.FindAsync(id);
                if (entity == null) return false;

                entity.TenHoaTiet = ht.TenHoaTiet;
                entity.MaHoaTiet = ht.MaHoaTiet;
                entity.TrangThai = ht.TrangThai;
                entity.LanCapNhatCuoi = DateTime.UtcNow;
                entity.NguoiCapNhat = string.IsNullOrEmpty(ht.NguoiCapNhat) ? "unknown" : ht.NguoiCapNhat;

                return await _context.SaveChangesAsync() > 0;
            }

            public async Task<bool> DeleteAsync(Guid id)
            {
                var entity = await _context.HoaTiet.FindAsync(id);
                if (entity == null) return false;

                _context.HoaTiet.Remove(entity);
                return await _context.SaveChangesAsync() > 0;
            }

            public async Task<bool> ToggleStatusAsync(Guid id)
            {
                var ht = await _context.HoaTiet.FindAsync(id);
                if (ht == null) return false;

                ht.TrangThai = !ht.TrangThai;
                ht.LanCapNhatCuoi = DateTime.UtcNow;
                ht.NguoiCapNhat = "auto-toggle";

                return await _context.SaveChangesAsync() > 0;
            }

            public async Task<(int total, List<HoaTiet> data)> GetPagedAsync(int page, int pageSize, string? keyword, string? trangThai)
            {
                var query = _context.HoaTiet.AsQueryable();

                if (!string.IsNullOrEmpty(keyword))
                    query = query.Where(x => x.TenHoaTiet.Contains(keyword) || x.MaHoaTiet.Contains(keyword));

                if (trangThai == "active")
                    query = query.Where(x => x.TrangThai);
                else if (trangThai == "inactive")
                    query = query.Where(x => !x.TrangThai);

                var total = await query.CountAsync();

                var data = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return (total, data);
            }
        }
    }


