using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using QuanApi.Services;

namespace QuanApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KichCoController : ControllerBase
    {
        private readonly IKichCoService _kichCoService;

        public KichCoController(IKichCoService kichCoService)
        {
            _kichCoService = kichCoService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(string? keyword)
            => Ok(await _kichCoService.GetAllAsync(keyword));

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var result = await _kichCoService.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create(KichCo kc)
        {
            var success = await _kichCoService.CreateAsync(kc);
            return success ? Ok(new { success = true }) : BadRequest();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] KichCo kc)
        {
            var success = await _kichCoService.UpdateAsync(id, kc);
            return success ? Ok(new { success = true }) : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _kichCoService.DeleteAsync(id);
            return success ? Ok(new { success = true }) : NotFound();
        }

        [HttpPut("ToggleStatus/{id}")]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var (success, newStatus) = await _kichCoService.ToggleStatusAsync(id);
            return success ? Ok(new { success = true, trangThai = newStatus }) : NotFound();
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(int page = 1, int pageSize = 10, string? keyword = null, string? trangThai = null)
        {
            var (total, data) = await _kichCoService.GetPagedAsync(page, pageSize, keyword, trangThai);
            return Ok(new { total, data });
        }
    }
}
