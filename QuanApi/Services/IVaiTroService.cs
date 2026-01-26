using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;

namespace QuanApi.Services
{
    public interface IVaiTroService
    {
        Task<IEnumerable<VaiTro>> GetAllAsync();
        Task<VaiTro?> GetByIdAsync(Guid id);
        Task<VaiTro> CreateAsync(VaiTro vaiTro);
        Task<bool> UpdateAsync(Guid id, VaiTro vaiTro);
        Task<bool> DeleteAsync(Guid id);
    }
    public class VaiTroService : IVaiTroService
    {
        private readonly BanQuanAu1DbContext _context;

        public VaiTroService(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<VaiTro>> GetAllAsync()
        {
            return await _context.VaiTro.ToListAsync();
        }

        public async Task<VaiTro?> GetByIdAsync(Guid id)
        {
            return await _context.VaiTro.FindAsync(id);
        }

        public async Task<VaiTro> CreateAsync(VaiTro vaiTro)
        {
            _context.VaiTro.Add(vaiTro);
            await _context.SaveChangesAsync();
            return vaiTro;
        }

        public async Task<bool> UpdateAsync(Guid id, VaiTro vaiTro)
        {
            if (id != vaiTro.IDVaiTro) return false;

            _context.Entry(vaiTro).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.VaiTro.Any(e => e.IDVaiTro == id)) return false;
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var vaiTro = await _context.VaiTro.FindAsync(id);
            if (vaiTro == null) return false;

            _context.VaiTro.Remove(vaiTro);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}