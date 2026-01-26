using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;

namespace QuanApi.Services
{
    public interface IGioHangService
    {
        Task<bool> AddToGioHangAsync(Guid idUser, Guid idSanPham, int soLuong);
        Task<GioHang?> GetByUserIdAsync(Guid userId);
        Task<bool> XoaChiTietGioHangAsync(Guid idChiTiet);
        Task<bool> UpdateChiTietGioHangAsync(Guid idChiTiet, int soLuong);
    }
    public class GioHangService : IGioHangService
    {
        private readonly BanQuanAu1DbContext _context;

        public GioHangService(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        public async Task<bool> AddToGioHangAsync(Guid idUser, Guid idSanPham, int soLuong)
        {
            var gioHang = await _context.GioHangs
                .Include(x => x.ChiTietGioHangs)
                .FirstOrDefaultAsync(x => x.IDKhachHang == idUser);

            if (gioHang == null)
            {
                gioHang = new GioHang
                {
                    IDGioHang = Guid.NewGuid(),
                    IDKhachHang = idUser,
                    MaGioHang = "GH_" + DateTime.Now.Ticks,
                    NgayTao = DateTime.Now
                };
                _context.GioHangs.Add(gioHang);
            }

            var chiTiet = gioHang.ChiTietGioHangs?
                .FirstOrDefault(x => x.IDSanPhamChiTiet == idSanPham);

            if (chiTiet == null)
            {
                chiTiet = new ChiTietGioHang
                {
                    IDChiTietGioHang = Guid.NewGuid(),
                    IDGioHang = gioHang.IDGioHang,
                    IDSanPhamChiTiet = idSanPham,
                    SoLuong = soLuong
                };
                _context.ChiTietGioHangs.Add(chiTiet);
            }
            else
            {
                chiTiet.SoLuong += soLuong;
            }

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<GioHang?> GetByUserIdAsync(Guid userId)
        {
            return await _context.GioHangs
                .Include(x => x.ChiTietGioHangs)
                    .ThenInclude(x => x.SanPhamChiTiet)
                .FirstOrDefaultAsync(x => x.IDKhachHang == userId);
        }

        public async Task<bool> XoaChiTietGioHangAsync(Guid idChiTiet)
        {
            var ct = await _context.ChiTietGioHangs.FindAsync(idChiTiet);
            if (ct == null) return false;

            _context.ChiTietGioHangs.Remove(ct);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateChiTietGioHangAsync(Guid idChiTiet, int soLuong)
        {
            var ct = await _context.ChiTietGioHangs.FindAsync(idChiTiet);
            if (ct == null) return false;

            ct.SoLuong = soLuong;
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
