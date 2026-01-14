using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;

namespace QuanApi.Services
{
    public interface IKhachHangPhieuGiamService
    {
        Task<List<KhachHangPhieuGiam>> GetAllAsync();
        Task<KhachHangPhieuGiam?> GetByIdAsync(Guid id);
        Task<Guid?> GetKhachHangByVoucherAsync(Guid idPhieu);
        Task<List<object>> GetPublicDiscountVouchersAsync();
        Task<List<object>> GetCustomerDiscountVouchersAsync(Guid customerId);
        Task<bool> CreateAsync(KhachHangPhieuGiam model);
        Task<bool> UpdateAsync(Guid id, KhachHangPhieuGiam model);
        Task<bool> DeleteAsync(Guid id);
    }

    public class KhachHangPhieuGiamService : IKhachHangPhieuGiamService
    {
        private readonly BanQuanAu1DbContext _context;

        public KhachHangPhieuGiamService(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        public async Task<List<KhachHangPhieuGiam>> GetAllAsync()
        {
            return await _context.KhachHangPhieuGiams
                .AsNoTracking()
                .Include(x => x.KhachHang)
                .Include(x => x.PhieuGiamGia)
                .ToListAsync();
        }

        public async Task<KhachHangPhieuGiam?> GetByIdAsync(Guid id)
        {
            return await _context.KhachHangPhieuGiams
                .Include(x => x.KhachHang)
                .Include(x => x.PhieuGiamGia)
                .FirstOrDefaultAsync(x => x.IDKhachHangPhieuGiam == id);
        }

        public async Task<Guid?> GetKhachHangByVoucherAsync(Guid idPhieu)
        {
            var item = await _context.KhachHangPhieuGiams
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IDPhieuGiamGia == idPhieu);

            return item?.IDKhachHang;
        }

        public async Task<List<object>> GetPublicDiscountVouchersAsync()
        {
            return await _context.PhieuGiamGias
                .Where(p =>
                    p.LaCongKhai &&
                    p.TrangThai &&
                    p.NgayBatDau <= DateTime.UtcNow &&
                    p.NgayKetThuc >= DateTime.UtcNow)
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
                    loaiPhieu = "Công khai",
                    trangThai = p.TrangThai
                })
                .Cast<object>()
                .ToListAsync();
        }

        public async Task<List<object>> GetCustomerDiscountVouchersAsync(Guid customerId)
        {
            return await _context.KhachHangPhieuGiams
                .Include(x => x.PhieuGiamGia)
                .Where(x =>
                    x.IDKhachHang == customerId &&
                    x.TrangThai &&
                    x.PhieuGiamGia.TrangThai &&
                    x.SoLuongDaSuDung < x.SoLuong &&
                    x.PhieuGiamGia.NgayBatDau <= DateTime.UtcNow &&
                    x.PhieuGiamGia.NgayKetThuc >= DateTime.UtcNow)
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
                .Cast<object>()
                .ToListAsync();
        }

        public async Task<bool> CreateAsync(KhachHangPhieuGiam model)
        {
            _context.KhachHangPhieuGiams.Add(model);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateAsync(Guid id, KhachHangPhieuGiam model)
        {
            if (id != model.IDKhachHangPhieuGiam)
                return false;

            _context.Entry(model).State = EntityState.Modified;
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _context.KhachHangPhieuGiams.FindAsync(id);
            if (entity == null) return false;

            _context.KhachHangPhieuGiams.Remove(entity);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
