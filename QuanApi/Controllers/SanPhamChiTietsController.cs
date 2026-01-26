using Microsoft.AspNetCore.Mvc;
using QuanApi.Dtos;
using QuanApi.Services;

namespace QuanApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SanPhamChiTietsController : ControllerBase
    {
        private readonly ISanPhamChiTietService _service;

        public SanPhamChiTietsController(ISanPhamChiTietService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpGet("bysanpham")]
        public async Task<IActionResult> GetBySanPham(Guid idsanpham)
            => Ok(await _service.GetBySanPhamAsync(idsanpham));

        [HttpPost]
        public async Task<IActionResult> Post(SanPhamChiTietDto dto)
        {
            var (merged, id, totalQuantity) = await _service.CreateAsync(dto);

            return merged
                ? Ok(new { message = "Biến thể đã tồn tại. Số lượng đã được cộng dồn.", id, totalQuantity })
                : CreatedAtAction(nameof(GetById), new { id }, dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(Guid id, SanPhamChiTietDto dto)
        {
            await _service.UpdateAsync(id, dto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }

        [HttpPut("bulk")]
        public async Task<IActionResult> PutBulk(List<SanPhamChiTietDto> dtos)
        {
            await _service.BulkUpdateAsync(dtos);
            return NoContent();
        }
    }

}
