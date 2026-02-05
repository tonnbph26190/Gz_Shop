using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using QuanApi.Services;

namespace QuanApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KieuDangController : ControllerBase
    {
        private readonly IKieuDangService _service;

        public KieuDangController(IKieuDangService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(string? keyword)
        {
            return Ok(await _service.GetAllAsync(keyword));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] KieuDang kd)
        {
            var success = await _service.CreateAsync(kd);
            return Ok(new { success });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] KieuDang kd)
        {
            var success = await _service.UpdateAsync(id, kd);
            return success ? Ok(new { success }) : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _service.DeleteAsync(id);
            return success ? Ok(new { success }) : NotFound();
        }

        [HttpPut("ToggleStatus/{id}")]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var success = await _service.ToggleStatusAsync(id);
            if (!success) return NotFound();
            var item = await _service.GetByIdAsync(id);
            return Ok(new { success = true, trangThai = item?.TrangThai });
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
