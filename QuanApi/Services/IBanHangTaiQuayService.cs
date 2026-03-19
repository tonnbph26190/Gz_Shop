using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;
public interface IBanHangTaiQuayService
{
    Task<List<HoaDon>> GetHoaDonChoAsync(Guid idNhanVien);
    Task<HoaDon?> GetHoaDonByIdAsync(Guid idHoaDon);
    Task<bool> TaoHoaDonAsync(Guid idNhanVien);
    Task<bool> ThemSanPhamAsync(Guid idHoaDon, Guid idSanPhamChiTiet, int soLuong);
    Task<bool> ThanhToanAsync(Guid idHoaDon);
}

public class BanHangTaiQuayService : IBanHangTaiQuayService
{
    private readonly BanQuanAu1DbContext _context;

    public BanHangTaiQuayService(BanQuanAu1DbContext context)
    {
        _context = context;
    }

    public async Task<List<HoaDon>> GetHoaDonChoAsync(Guid idNhanVien)
    {
        return await _context.HoaDons
            .Where(x => x.IDNhanVien == idNhanVien && x.TrangThai == "ChoThanhToan")
            .Include(x => x.ChiTietHoaDons)
            .OrderByDescending(x => x.NgayTao)
            .ToListAsync();
    }

    public async Task<HoaDon?> GetHoaDonByIdAsync(Guid idHoaDon)
    {
        return await _context.HoaDons
            .Include(x => x.ChiTietHoaDons)
                .ThenInclude(x => x.SanPhamChiTiet)
            .FirstOrDefaultAsync(x => x.IDHoaDon == idHoaDon);
    }

    public async Task<bool> TaoHoaDonAsync(Guid idNhanVien)
    {
        var hoaDon = new HoaDon
        {
            IDHoaDon = Guid.NewGuid(),
            IDNhanVien = idNhanVien,
            NgayTao = DateTime.UtcNow,
            BanTaiQuay = true,
            TrangThai = "ChoThanhToan"
        };

        _context.HoaDons.Add(hoaDon);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> ThemSanPhamAsync(Guid idHoaDon, Guid idSanPhamChiTiet, int soLuong)
    {
        var ct = await _context.ChiTietHoaDons
            .FirstOrDefaultAsync(x =>
                x.IDHoaDon == idHoaDon &&
                x.IDSanPhamChiTiet == idSanPhamChiTiet);

        if (ct == null)
        {
            ct = new ChiTietHoaDon
            {
                IDChiTietHoaDon = Guid.NewGuid(),
                IDHoaDon = idHoaDon,
                IDSanPhamChiTiet = idSanPhamChiTiet,
                SoLuong = soLuong
            };
            _context.ChiTietHoaDons.Add(ct);
        }
        else
        {
            ct.SoLuong += soLuong;
        }

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> ThanhToanAsync(Guid idHoaDon)
    {
        var hoaDon = await _context.HoaDons.FindAsync(idHoaDon);
        if (hoaDon == null) return false;

        hoaDon.TrangThai = "DaThanhToan";
        hoaDon.LanCapNhatCuoi = DateTime.UtcNow;

        return await _context.SaveChangesAsync() > 0;
    }
}
