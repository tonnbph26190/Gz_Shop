using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using QuanApi.Services;

namespace QuanApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DotGiamGiasController : ControllerBase
    {
        private readonly IDotGiamGiaService _service;

        public DotGiamGiasController(IDotGiamGiaService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(string? keyword)
            => Ok(await _service.GetAllAsync(keyword));

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] DotGiamGia dot,
            [FromQuery] List<Guid> sanPhamChiTietIds)
        {
            var success = await _service.CreateAsync(dot, sanPhamChiTietIds);
            return success ? Ok(new { success = true }) : BadRequest();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] DotGiamGia dot,
            [FromQuery] List<Guid> sanPhamChiTietIds)
        {
            var success = await _service.UpdateAsync(id, dot, sanPhamChiTietIds);
            return success ? Ok(new { success = true }) : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _service.DeleteAsync(id);
            return success ? Ok(new { success = true }) : NotFound();
        }

        [HttpPut("ToggleStatus/{id}")]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var success = await _service.ToggleStatusAsync(id);
            return success ? Ok(new { success = true }) : NotFound();
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(
            int page = 1,
            int pageSize = 10,
            string? keyword = null,
            string? trangThai = null)
        {
            var (total, data) = await _service.GetPagedAsync(page, pageSize, keyword, trangThai);
            return Ok(new { total, data });
        }
    }
}
