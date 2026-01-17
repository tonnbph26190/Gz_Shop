using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using QuanApi.Dtos;
using QuanApi.Services;

[ApiController]
[Route("api/[controller]")]
public class SanPhamsController : ControllerBase
{
    private readonly ISanPhamService _service;

    public SanPhamsController(ISanPhamService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetSanPhams()
        => Ok(await _service.GetAllAsync());

    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        string? keyword = null, string? trangThai = null,
        decimal? priceFrom = null, decimal? priceTo = null,
        int? qtyFrom = null, int? qtyTo = null,
        DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        var result = await _service.GetPagedAsync(page, pageSize, keyword, trangThai,
            priceFrom, priceTo, qtyFrom, qtyTo, dateFrom, dateTo);

        return Ok(new { result.total, result.data });
    }

    [HttpPost]
    public async Task<IActionResult> PostSanPham(SanPham sp)
        => await _service.CreateAsync(sp, User.Identity?.Name ?? "System");

    [HttpPut("{id}")]
    public async Task<IActionResult> PutSanPham(Guid id, SanPham sp)
        => await _service.UpdateAsync(id, sp);

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSanPham(Guid id)
        => await _service.DeleteAsync(id) ? NoContent() : NotFound();

    [HttpGet("{id}")]
    public async Task<ActionResult<SanPham>> GetSanPham(Guid id)
    {
        var sanPham = await _service.GetByIdAsync(id);

        if (sanPham == null)
            return NotFound();

        return sanPham;
    }


    [HttpPost("chitiet/{sanPhamChiTietId}/images")]
    public async Task<IActionResult> AddProductImage(
    Guid sanPhamChiTietId,
    [FromBody] AddAnhSanPhamDto dto)
    {
        var result = await _service.AddProductImageAsync(
            sanPhamChiTietId,
            dto,
            User.Identity?.Name ?? "System");

        return Ok(new
        {
            message = "Thêm ảnh sản phẩm chi tiết thành công.",
            result.IDAnhSanPham,
            result.MaAnh,
            result.UrlAnh,
            result.LaAnhChinh
        });
    }

    [HttpDelete("images/{imageId}")]
    public async Task<IActionResult> DeleteProductImage(Guid imageId)
    {
        await _service.DeleteProductImageAsync(
            imageId,
            User.Identity?.Name ?? "System");

        return Ok("Đã xóa ảnh sản phẩm.");
    }

    [HttpPut("images/{imageId}/set-main")]
    public async Task<IActionResult> SetMainImage(Guid imageId)
    {
        await _service.SetMainImageAsync(
            imageId,
            User.Identity?.Name ?? "System");

        return Ok("Đã đặt ảnh làm ảnh chính.");
    }

    [HttpGet("chitiet/{sanPhamChiTietId}/images")]
    public async Task<IActionResult> GetProductImages(Guid sanPhamChiTietId)
    {
        var images = await _service.GetProductImagesAsync(sanPhamChiTietId);
        return Ok(images);
    }

    [HttpPost("chitiet/{sanPhamChiTietId}/upload-image")]
    public async Task<IActionResult> UploadProductImage(
        Guid sanPhamChiTietId,
        IFormFile file,
        bool laAnhChinh = false)
    {
        var result = await _service.UploadProductImageAsync(
            sanPhamChiTietId,
            file,
            laAnhChinh,
            User.Identity?.Name ?? "System");

        return Ok(new
        {
            message = "Upload ảnh thành công.",
            result.UrlAnh,
            result.IDAnhSanPham,
            result.MaAnh,
            result.LaAnhChinh
        });
    }

}
