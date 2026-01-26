using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;

namespace QuanApi.Services
{
    public interface IPhuongThucThanhToanService
    {
        Task<List<PhuongThucThanhToan>> GetAllAsync();
        Task<PhuongThucThanhToan?> GetByIdAsync(Guid id);
        Task<PhuongThucThanhToan> CreateAsync(PhuongThucThanhToan entity);
        Task<bool> UpdateAsync(Guid id, PhuongThucThanhToan entity);
        Task<bool> DeleteAsync(Guid id);
    }
    public class PhuongThucThanhToanService : IPhuongThucThanhToanService
    {
        private readonly BanQuanAu1DbContext _context;

        public PhuongThucThanhToanService(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        public async Task<List<PhuongThucThanhToan>> GetAllAsync()
        {
            return await _context.PhuongThucThanhToans
                .Where(x => x.TrangThai)
                .ToListAsync();
        }

        public async Task<PhuongThucThanhToan?> GetByIdAsync(Guid id)
        {
            return await _context.PhuongThucThanhToans.FindAsync(id);
        }

        public async Task<PhuongThucThanhToan> CreateAsync(PhuongThucThanhToan entity)
        {
            entity.IDPhuongThucThanhToan = Guid.NewGuid();
            entity.TrangThai = true;

            _context.PhuongThucThanhToans.Add(entity);
            await _context.SaveChangesAsync();

            return entity;
        }

        public async Task<bool> UpdateAsync(Guid id, PhuongThucThanhToan entity)
        {
            var pt = await _context.PhuongThucThanhToans.FindAsync(id);
            if (pt == null) return false;

            pt.TenPhuongThuc = entity.TenPhuongThuc;
            pt.MoTa = entity.MoTa;
            pt.TrangThai = entity.TrangThai;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var pt = await _context.PhuongThucThanhToans.FindAsync(id);
            if (pt == null) return false;

            _context.PhuongThucThanhToans.Remove(pt);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
