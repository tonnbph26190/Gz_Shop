using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;

namespace QuanApi.Services
{
    public interface IPhieuGiamGiaService
    {
        Task<List<PhieuGiamGia>> GetAllAsync();
        Task<PhieuGiamGia?> GetByIdAsync(Guid id);
        Task<bool> CreateAsync(PhieuGiamGia model, string? nguoiTao);
        Task<bool> UpdateAsync(Guid id, PhieuGiamGia model, string? nguoiCapNhat);
        Task<bool> DeleteAsync(Guid id);

        Task<bool> AddCustomerAsync(Guid voucherId, Guid customerId, string? nguoiTao);
        Task<bool> RemoveCustomerAsync(Guid voucherId, Guid customerId);

        Task<bool> UseVoucherAsync(Guid voucherId, Guid customerId, string? nguoiDung);
        Task<bool> ResetUsageAsync(Guid voucherId, Guid customerId, string? nguoiDung);

        Task<object?> KiemTraMaAsync(string code);
        Task<List<object>> GetVoucherCustomersAsync(Guid voucherId);
    }

    public class PhieuGiamGiaService : IPhieuGiamGiaService
    {
        private readonly BanQuanAu1DbContext _context;

        public PhieuGiamGiaService(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        public async Task<List<PhieuGiamGia>> GetAllAsync()
        {
            return await _context.PhieuGiamGias
                .OrderByDescending(x => x.NgayTao)
                .ToListAsync();
        }

        public async Task<PhieuGiamGia?> GetByIdAsync(Guid id)
            => await _context.PhieuGiamGias.FindAsync(id);

        public async Task<bool> CreateAsync(PhieuGiamGia model, string? nguoiTao)
        {
            model.IDPhieuGiamGia = Guid.NewGuid();
            model.NgayTao = DateTime.UtcNow;
            model.LaCongKhai = true;
            model.SoLuong = 1;
            model.NguoiTao = nguoiTao ?? "System";

            _context.PhieuGiamGias.Add(model);
            await _context.SaveChangesAsync();

            var customers = await _context.KhachHang.Where(x => x.TrangThai).ToListAsync();

            var links = customers.Select(c => new KhachHangPhieuGiam
            {
                IDKhachHangPhieuGiam = Guid.NewGuid(),
                MaKhachHangPhieuGiam = $"KHPG_{DateTime.Now:yyyyMMddHHmmss}_{c.IDKhachHang.ToString()[..8]}",
                IDKhachHang = c.IDKhachHang,
                IDPhieuGiamGia = model.IDPhieuGiamGia,
                SoLuong = 1,
                SoLuongDaSuDung = 0,
                NgayTao = DateTime.UtcNow,
                NguoiTao = nguoiTao ?? "System",
                TrangThai = true
            }).ToList();

            _context.KhachHangPhieuGiams.AddRange(links);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateAsync(Guid id, PhieuGiamGia model, string? nguoiCapNhat)
        {
            if (id != model.IDPhieuGiamGia) return false;

            model.LaCongKhai = true;
            model.SoLuong = 1;
            model.LanCapNhatCuoi = DateTime.UtcNow;
            model.NguoiCapNhat = nguoiCapNhat ?? "System";

            _context.Entry(model).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            var oldLinks = await _context.KhachHangPhieuGiams
                .Where(x => x.IDPhieuGiamGia == id)
                .ToListAsync();

            _context.KhachHangPhieuGiams.RemoveRange(oldLinks);

            var customers = await _context.KhachHang.Where(x => x.TrangThai).ToListAsync();

            var links = customers.Select(c => new KhachHangPhieuGiam
            {
                IDKhachHangPhieuGiam = Guid.NewGuid(),
                MaKhachHangPhieuGiam = $"KHPG_{DateTime.Now:yyyyMMddHHmmss}_{c.IDKhachHang.ToString()[..8]}",
                IDKhachHang = c.IDKhachHang,
                IDPhieuGiamGia = id,
                SoLuong = 1,
                SoLuongDaSuDung = 0,
                NgayTao = DateTime.UtcNow,
                NguoiTao = nguoiCapNhat ?? "System",
                TrangThai = true
            }).ToList();

            _context.KhachHangPhieuGiams.AddRange(links);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _context.PhieuGiamGias.FindAsync(id);
            if (entity == null) return false;

            var links = await _context.KhachHangPhieuGiams
                .Where(x => x.IDPhieuGiamGia == id)
                .ToListAsync();

            _context.KhachHangPhieuGiams.RemoveRange(links);
            _context.PhieuGiamGias.Remove(entity);

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> AddCustomerAsync(Guid voucherId, Guid customerId, string? nguoiTao)
        {
            if (await _context.KhachHangPhieuGiams
                .AnyAsync(x => x.IDPhieuGiamGia == voucherId && x.IDKhachHang == customerId))
                return false;

            _context.KhachHangPhieuGiams.Add(new KhachHangPhieuGiam
            {
                IDKhachHangPhieuGiam = Guid.NewGuid(),
                MaKhachHangPhieuGiam = $"KHPG_{DateTime.Now:yyyyMMddHHmmss}_{customerId.ToString()[..8]}",
                IDKhachHang = customerId,
                IDPhieuGiamGia = voucherId,
                SoLuong = 1,
                NgayTao = DateTime.UtcNow,
                NguoiTao = nguoiTao ?? "System",
                TrangThai = true
            });

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> RemoveCustomerAsync(Guid voucherId, Guid customerId)
        {
            var link = await _context.KhachHangPhieuGiams
                .FirstOrDefaultAsync(x => x.IDPhieuGiamGia == voucherId && x.IDKhachHang == customerId);

            if (link == null) return false;

            _context.KhachHangPhieuGiams.Remove(link);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UseVoucherAsync(Guid voucherId, Guid customerId, string? nguoiDung)
        {
            var cv = await _context.KhachHangPhieuGiams
                .Include(x => x.PhieuGiamGia)
                .FirstOrDefaultAsync(x => x.IDPhieuGiamGia == voucherId && x.IDKhachHang == customerId);

            if (cv == null || !cv.TrangThai || cv.SoLuongDaSuDung >= cv.SoLuong)
                return false;

            var now = DateTime.UtcNow;
            if (now < cv.PhieuGiamGia.NgayBatDau || now > cv.PhieuGiamGia.NgayKetThuc)
                return false;

            cv.SoLuongDaSuDung++;
            cv.LanCapNhatCuoi = now;
            cv.NguoiCapNhat = nguoiDung ?? "System";

            if (cv.SoLuongDaSuDung >= cv.SoLuong)
                cv.TrangThai = false;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> ResetUsageAsync(Guid voucherId, Guid customerId, string? nguoiDung)
        {
            var cv = await _context.KhachHangPhieuGiams
                .FirstOrDefaultAsync(x => x.IDPhieuGiamGia == voucherId && x.IDKhachHang == customerId);

            if (cv == null) return false;

            cv.SoLuongDaSuDung = 0;
            cv.TrangThai = true;
            cv.LanCapNhatCuoi = DateTime.UtcNow;
            cv.NguoiCapNhat = nguoiDung ?? "System";

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<object?> KiemTraMaAsync(string code)
        {
            var p = await _context.PhieuGiamGias
                .FirstOrDefaultAsync(x => x.MaCode == code && x.TrangThai);

            if (p == null) return null;

            return new
            {
                p.GiaTriGiam,
                p.GiaTriGiamToiDa,
                p.DonToiThieu
            };
        }

        public async Task<List<object>> GetVoucherCustomersAsync(Guid voucherId)
        {
            return await _context.KhachHangPhieuGiams
                .Include(x => x.KhachHang)
                .Where(x => x.IDPhieuGiamGia == voucherId)
                .Select(x => new
                {
                    x.KhachHang.MaKhachHang,
                    x.KhachHang.TenKhachHang,
                    x.KhachHang.Email,
                    x.SoLuong,
                    x.SoLuongDaSuDung,
                    SoLuongConLai = x.SoLuong - x.SoLuongDaSuDung
                })
                .Cast<object>()
                .ToListAsync();
        }
    }
}
