using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;
using System;

namespace QuanApi.Services
{
    public interface IKhachHangPhieuGiamService
    {
        Task<List<KhachHangPhieuGiam>> GetAllAsync();
        Task<KhachHangPhieuGiam?> GetByIdAsync(Guid id);
        Task<Guid?> GetKhachHangByVoucherAsync(Guid idPhieu);
        Task<List<object>> GetPublicDiscountVouchersAsync();
        Task<List<object>> GetCustomerDiscountVouchersAsync(Guid customerId);
        Task<KhachHangPhieuGiam> CreateAsync(KhachHangPhieuGiam model);
        Task<bool> UpdateAsync(Guid id, KhachHangPhieuGiam model);
        Task<bool> DeleteAsync(Guid id);
    }

    public class KhachHangPhieuGiamService : IKhachHangPhieuGiamService
    {
        private readonly BanQuanAu1DbContext _context;
        private readonly ILogger<KhachHangPhieuGiamService> _logger;

        public KhachHangPhieuGiamService(
            BanQuanAu1DbContext context,
            ILogger<KhachHangPhieuGiamService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<KhachHangPhieuGiam>> GetAllAsync()
        {
            _logger.LogInformation("Lấy toàn bộ KhachHangPhieuGiam");

            return await _context.KhachHangPhieuGiams
                .AsNoTracking()
                .Include(k => k.KhachHang)
                .Include(k => k.PhieuGiamGia)
                .ToListAsync();
        }

        public async Task<KhachHangPhieuGiam?> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Lấy KhachHangPhieuGiam theo ID: {Id}", id);

            return await _context.KhachHangPhieuGiams
                .Include(k => k.KhachHang)
                .Include(k => k.PhieuGiamGia)
                .FirstOrDefaultAsync(x => x.IDKhachHangPhieuGiam == id);
        }
         public async Task<Guid?> GetKhachHangByVoucherAsync(Guid idPhieu)
    {
        _logger.LogInformation("Lấy khách hàng theo phiếu giảm giá: {IdPhieu}", idPhieu);

        var item = await _context.KhachHangPhieuGiams
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IDPhieuGiamGia == idPhieu);

        return item?.IDKhachHang;
    }

         public async Task<List<object>> GetPublicDiscountVouchersAsync()
    {
        _logger.LogInformation("Lấy danh sách phiếu giảm giá công khai");

        var now = DateTime.UtcNow;

        return await _context.PhieuGiamGias
            .Where(p => p.LaCongKhai == true &&
                        p.TrangThai &&
                        p.NgayBatDau <= now &&
                        p.NgayKetThuc >= now)
            .Select(p => new
            {
                id = p.IDPhieuGiamGia,
                maCode = p.MaCode,
                tenPhieu = p.TenPhieu,
                giaTriGiam = p.GiaTriGiam,
                giaTriGiamToiDa = p.GiaTriGiamToiDa,
                donToiThieu = p.DonToiThieu,
                ngayBatDau = p.NgayBatDau,
                ngayKetThuc = p.NgayKetThuc,
                soLuong = p.SoLuong,
                loaiPhieu = p.LaCongKhai ? "Công khai" : "Riêng tư",
                trangThai = p.TrangThai
            })
            .ToListAsync<object>();
    }
        public async Task<List<object>> GetCustomerDiscountVouchersAsync(Guid customerId)
        {
            _logger.LogInformation("Lấy phiếu giảm giá của khách hàng: {CustomerId}", customerId);

            var now = DateTime.UtcNow;

            return await _context.KhachHangPhieuGiams
                .Include(k => k.PhieuGiamGia)
                .Where(x => x.IDKhachHang == customerId &&
                            x.TrangThai &&
                            x.PhieuGiamGia.TrangThai &&
                            x.SoLuongDaSuDung < x.SoLuong &&
                            x.PhieuGiamGia.NgayBatDau <= now &&
                            x.PhieuGiamGia.NgayKetThuc >= now)
                .Select(x => new
                {
                    id = x.IDPhieuGiamGia,
                    maCode = x.PhieuGiamGia.MaCode,
                    tenPhieu = x.PhieuGiamGia.TenPhieu,
                    giaTriGiam = x.PhieuGiamGia.GiaTriGiam,
                    giaTriGiamToiDa = x.PhieuGiamGia.GiaTriGiamToiDa,
                    donToiThieu = x.PhieuGiamGia.DonToiThieu,
                    ngayBatDau = x.PhieuGiamGia.NgayBatDau,
                    ngayKetThuc = x.PhieuGiamGia.NgayKetThuc,
                    soLuong = x.SoLuong,
                    soLuongDaSuDung = x.SoLuongDaSuDung,
                    soLuongConLai = x.SoLuong - x.SoLuongDaSuDung,
                    loaiPhieu = x.PhieuGiamGia.LaCongKhai ? "Công khai" : "Riêng tư"
                })
                .ToListAsync<object>();
        }

        public async Task<KhachHangPhieuGiam> CreateAsync(KhachHangPhieuGiam model)
        {
            _logger.LogInformation("Tạo KhachHangPhieuGiam mới");

            _context.KhachHangPhieuGiams.Add(model);
            await _context.SaveChangesAsync();

            return model;
        }
        public async Task<bool> UpdateAsync(Guid id, KhachHangPhieuGiam model)
        {
            _logger.LogInformation("Cập nhật KhachHangPhieuGiam ID: {Id}", id);

            var exists = await _context.KhachHangPhieuGiams
                .AnyAsync(e => e.IDKhachHangPhieuGiam == id);

            if (!exists)
                return false;

            _context.Entry(model).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogWarning("Concurrency khi cập nhật KhachHangPhieuGiam ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation("Xóa KhachHangPhieuGiam ID: {Id}", id);

            var entity = await _context.KhachHangPhieuGiams.FindAsync(id);
            if (entity == null)
                return false;

            _context.KhachHangPhieuGiams.Remove(entity);
            await _context.SaveChangesAsync();

            return true;
        }

    }
}
