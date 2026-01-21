using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;
public interface IHoaDonService
{
    Task<List<HoaDon>> GetAllAsync();
    Task<HoaDon?> GetByIdAsync(Guid id);
    Task<HoaDon> CreateAsync(HoaDon hoaDon);
    Task<bool> UpdateTrangThaiAsync(Guid id, string trangThai);
    Task<bool> DeleteAsync(Guid id);
}
public class HoaDonService : IHoaDonService
{
    private readonly BanQuanAu1DbContext _context;

    public HoaDonService(BanQuanAu1DbContext context)
    {
        _context = context;
    }

    public async Task<List<HoaDon>> GetAllAsync()
    {
        return await _context.HoaDons
            .Include(x => x.ChiTietHoaDons)
            .ToListAsync();
    }

    public async Task<HoaDon?> GetByIdAsync(Guid id)
    {
        return await _context.HoaDons
            .Include(x => x.ChiTietHoaDons)
            .FirstOrDefaultAsync(x => x.IDHoaDon == id);
    }

    public async Task<HoaDon> CreateAsync(HoaDon hoaDon)
    {
        hoaDon.IDHoaDon = Guid.NewGuid();
        hoaDon.NgayTao = DateTime.Now;
        hoaDon.TrangThai ??= "Chờ xác nhận";

        _context.HoaDons.Add(hoaDon);
        await _context.SaveChangesAsync();

        return hoaDon;
    }

    public async Task<bool> UpdateTrangThaiAsync(Guid id, string trangThai)
    {
        var hoaDon = await _context.HoaDons.FindAsync(id);
        if (hoaDon == null) return false;

        hoaDon.TrangThai = trangThai;
        hoaDon.LanCapNhatCuoi = DateTime.Now;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var hoaDon = await _context.HoaDons.FindAsync(id);
        if (hoaDon == null) return false;

        _context.HoaDons.Remove(hoaDon);
        await _context.SaveChangesAsync();
        return true;
    }
}
