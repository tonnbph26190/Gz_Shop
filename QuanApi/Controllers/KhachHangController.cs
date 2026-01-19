using Azure;
using Microsoft.AspNetCore.Mvc;
using QuanApi.Services;

[ApiController]
[Route("api/[controller]")]
public class KhachHangController : ControllerBase
{
    private readonly IKhachHangService _service;

    public KhachHangController(IKhachHangService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        string? search = null,
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = "NgayTao",
        bool sortAscending = false)
    {
        var (data, total) = await _service.GetAllAsync(search, pageNumber, pageSize, sortBy, sortAscending);

        Response.Headers.Append("X-Total-Count", total.ToString());
        return Ok(data);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var kh = await _service.GetByIdAsync(id);
        if (kh == null) return NotFound();
        return Ok(kh);
    }

    [HttpPost]
    public async Task<IActionResult> Create(KhachHang dto)
    {
        var result = await _service.CreateAsync(dto, User.Identity?.Name);
        return CreatedAtAction(nameof(GetById), new { id = result.IDKhachHang }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, KhachHang dto)
    {
        if (id != dto.IDKhachHang) return BadRequest();

        var ok = await _service.UpdateAsync(id, dto, User.Identity?.Name);
        if (!ok) return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ok = await _service.DeleteAsync(id);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpGet("{id}/addresses")]
    public async Task<IActionResult> GetAddresses(Guid id)
    {
        var data = await _service.GetAddressesAsync(id);
        return Ok(data);
    }

    [HttpGet("{id}/default-address")]
    public async Task<IActionResult> GetDefaultAddress(Guid id)
    {
        var dc = await _service.GetDefaultAddressAsync(id);
        if (dc == null) return NotFound();
        return Ok(dc);
    }
}
