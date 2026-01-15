using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;

[ApiController]
[Route("api/[controller]")]
public class HoaDonsController : ControllerBase
{
    private readonly IHoaDonService _service;

    public HoaDonsController(IHoaDonService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(HoaDon hoaDon)
    {
        return Ok(await _service.CreateAsync(hoaDon));
    }

    [HttpPut("{id}/trangthai")]
    public async Task<IActionResult> UpdateTrangThai(Guid id, string trangThai)
    {
        var result = await _service.UpdateTrangThaiAsync(id, trangThai);
        return result ? Ok() : NotFound();
    }
}
