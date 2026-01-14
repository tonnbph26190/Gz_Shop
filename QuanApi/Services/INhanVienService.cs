using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;

namespace QuanApi.Services
{
    public interface INhanVienService
    {
        Task<List<NhanVien>> GetAllAsync(string? keyword);
        Task<NhanVien?> GetByIdAsync(Guid id);
        Task<bool> CreateAsync(NhanVien nv);
        Task<bool> UpdateAsync(Guid id, NhanVien nv);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ToggleStatusAsync(Guid id);
        Task<(int total, List<NhanVien> data)> GetPagedAsync(int page, int pageSize, string? keyword, string? trangThai);
    }
    public class NhanVienService : INhanVienService
    {
        private readonly BanQuanAu1DbContext _context;

        public NhanVienService(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        public async Task<List<NhanVien>> GetAllAsync(string? keyword)
        {
            var query = _context.NhanViens.AsQueryable();
            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(x => x.TenNhanVien.Contains(keyword) || x.MaNhanVien.Contains(keyword));

            return await query.ToListAsync();
        }

        public async Task<NhanVien?> GetByIdAsync(Guid id)
        {
            return await _context.NhanViens.FindAsync(id);
        }

        public async Task<bool> CreateAsync(NhanVien nv)
        {
            nv.IDNhanVien = Guid.NewGuid();
            nv.NgayTao = DateTime.Now;
            if (string.IsNullOrEmpty(nv.NguoiTao)) nv.NguoiTao = "unknown";

            _context.NhanViens.Add(nv);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateAsync(Guid id, NhanVien nv)
        {
            var entity = await _context.NhanViens.FindAsync(id);
            if (entity == null) return false;

            entity.TenNhanVien = nv.TenNhanVien;
            entity.MaNhanVien = nv.MaNhanVien;
            entity.Email = nv.Email;
            entity.MatKhau = nv.MatKhau;
            entity.NgaySinh = nv.NgaySinh;
            entity.GioiTinh = nv.GioiTinh;
            entity.QueQuan = nv.QueQuan;
            entity.CCCD = nv.CCCD;
            entity.SoDienThoai = nv.SoDienThoai;
            entity.NgayTao = nv.NgayTao;
            entity.LanCapNhatCuoi = DateTime.Now;
            entity.NguoiCapNhat = string.IsNullOrEmpty(nv.NguoiCapNhat) ? "unknown" : nv.NguoiCapNhat;
            entity.TrangThai = nv.TrangThai;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _context.NhanViens.FindAsync(id);
            if (entity == null) return false;

            _context.NhanViens.Remove(entity);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> ToggleStatusAsync(Guid id)
        {
            var nv = await _context.NhanViens.FindAsync(id);
            if (nv == null) return false;

            nv.TrangThai = !nv.TrangThai;
            nv.LanCapNhatCuoi = DateTime.Now;
            nv.NguoiCapNhat = "auto-toggle";

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<(int total, List<NhanVien> data)> GetPagedAsync(int page, int pageSize, string? keyword, string? trangThai)
        {
            var query = _context.NhanViens.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(x => x.TenNhanVien.Contains(keyword) || x.MaNhanVien.Contains(keyword));

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
