using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class BanHangTaiQuayController : ControllerBase
{
    private readonly IBanHangTaiQuayService _service;

    public BanHangTaiQuayController(IBanHangTaiQuayService service)
    {
        _service = service;
    }

    // 1. Lấy hóa đơn chờ theo nhân viên
    [HttpGet("hoa-don-cho/{idNhanVien}")]
    public async Task<IActionResult> GetHoaDonCho(Guid idNhanVien)
    {
        var data = await _service.GetHoaDonChoAsync(idNhanVien);
        return Ok(data);
    }

    // 2. Lấy chi tiết hóa đơn
    [HttpGet("hoa-don/{idHoaDon}")]
    public async Task<IActionResult> GetHoaDon(Guid idHoaDon)
    {
        var hoaDon = await _service.GetHoaDonByIdAsync(idHoaDon);
        if (hoaDon == null) return NotFound();
        return Ok(hoaDon);
    }

    // 3. Tạo hóa đơn mới
    [HttpPost("tao-hoa-don")]
    public async Task<IActionResult> TaoHoaDon(Guid idNhanVien)
    {
        var result = await _service.TaoHoaDonAsync(idNhanVien);
        return result ? Ok("Tạo hóa đơn thành công") : BadRequest();
    }

    // 4. Thêm sản phẩm vào hóa đơn
    [HttpPost("them-san-pham")]
    public async Task<IActionResult> ThemSanPham(Guid idHoaDon, Guid idSanPhamChiTiet, int soLuong)
    {
        var result = await _service.ThemSanPhamAsync(idHoaDon, idSanPhamChiTiet, soLuong);
        return result ? Ok("Thêm sản phẩm thành công") : BadRequest();
    }

    // 5. Thanh toán
    [HttpPost("thanh-toan/{idHoaDon}")]
    public async Task<IActionResult> ThanhToan(Guid idHoaDon)
    {
        var result = await _service.ThanhToanAsync(idHoaDon);
        return result ? Ok("Thanh toán thành công") : BadRequest();
    }
}
