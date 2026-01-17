using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;

namespace QuanApi.Services
{
    public interface ILungQuanService
    {
        Task<IEnumerable<LungQuan>> GetAllAsync(string? keyword);
        Task<LungQuan?> GetByIdAsync(Guid id);
        Task<bool> CreateAsync(LungQuan lq);
        Task<bool> UpdateAsync(Guid id, LungQuan lq);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ToggleStatusAsync(Guid id);
        Task<(int total, List<LungQuan> data)> GetPagedAsync(int page, int pageSize, string? keyword, string? trangThai);
    }
    
        public class LungQuanService : ILungQuanService
        {
            private readonly BanQuanAu1DbContext _context;

            public LungQuanService(BanQuanAu1DbContext context)
            {
                _context = context;
            }

            public async Task<IEnumerable<LungQuan>> GetAllAsync(string? keyword)
            {
                var query = _context.LungQuans.AsQueryable();

                if (!string.IsNullOrEmpty(keyword))
                    query = query.Where(x => x.TenLungQuan.Contains(keyword) || x.MaLungQuan.Contains(keyword));

                return await query.ToListAsync();
            }

            public async Task<LungQuan?> GetByIdAsync(Guid id)
            {
                return await _context.LungQuans.FindAsync(id);
            }

            public async Task<bool> CreateAsync(LungQuan lq)
            {
                lq.IDLungQuan = Guid.NewGuid();
                lq.NgayTao = DateTime.Now;
                lq.NguoiTao = string.IsNullOrEmpty(lq.NguoiTao) ? "unknown" : lq.NguoiTao;

                _context.LungQuans.Add(lq);
                return await _context.SaveChangesAsync() > 0;
            }

            public async Task<bool> UpdateAsync(Guid id, LungQuan lq)
            {
                var entity = await _context.LungQuans.FindAsync(id);
                if (entity == null) return false;

                entity.TenLungQuan = lq.TenLungQuan;
                entity.MaLungQuan = lq.MaLungQuan;
                entity.TrangThai = lq.TrangThai;
                entity.LanCapNhatCuoi = DateTime.Now;
                entity.NguoiCapNhat = string.IsNullOrEmpty(lq.NguoiCapNhat) ? "unknown" : lq.NguoiCapNhat;

                return await _context.SaveChangesAsync() > 0;
            }

            public async Task<bool> DeleteAsync(Guid id)
            {
                var entity = await _context.LungQuans.FindAsync(id);
                if (entity == null) return false;

                _context.LungQuans.Remove(entity);
                return await _context.SaveChangesAsync() > 0;
            }

            public async Task<bool> ToggleStatusAsync(Guid id)
            {
                var lq = await _context.LungQuans.FindAsync(id);
                if (lq == null) return false;

                lq.TrangThai = !lq.TrangThai;
                lq.LanCapNhatCuoi = DateTime.Now;
                lq.NguoiCapNhat = "auto-toggle";

                return await _context.SaveChangesAsync() > 0;
            }

            public async Task<(int total, List<LungQuan> data)> GetPagedAsync(int page, int pageSize, string? keyword, string? trangThai)
            {
                var query = _context.LungQuans.AsQueryable();

                if (!string.IsNullOrEmpty(keyword))
                    query = query.Where(x => x.TenLungQuan.Contains(keyword) || x.MaLungQuan.Contains(keyword));

                if (trangThai == "active")
                    query = query.Where(x => x.TrangThai);
                else if (trangThai == "inactive")
                    query = query.Where(x => !x.TrangThai);

                int total = await query.CountAsync();

                var data = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return (total, data);
            }
        }
    }


