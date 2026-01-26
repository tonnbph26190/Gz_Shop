using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using QuanApi.Services;

namespace QuanApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MauSacController : ControllerBase
    {
        private readonly IMauSacService _mauSacService;

        public MauSacController(IMauSacService mauSacService)
        {
            _mauSacService = mauSacService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(string? keyword)
            => Ok(await _mauSacService.GetAllAsync(keyword));

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var result = await _mauSacService.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create(MauSac ms)
        {
            var success = await _mauSacService.CreateAsync(ms);
            return success ? Ok(new { success = true }) : BadRequest();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] MauSac ms)
        {
            var success = await _mauSacService.UpdateAsync(id, ms);
            return success ? Ok(new { success = true }) : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _mauSacService.DeleteAsync(id);
            return success ? Ok(new { success = true }) : NotFound();
        }

        [HttpPut("ToggleStatus/{id}")]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var (success, newStatus) = await _mauSacService.ToggleStatusAsync(id);
            return success ? Ok(new { success = true, trangThai = newStatus }) : NotFound();
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(int page = 1, int pageSize = 10, string? keyword = null, string? trangThai = null)
        {
            var result = await _mauSacService.GetPagedAsync(page, pageSize, keyword, trangThai);
            return Ok(new { total = result.Total, data = result.Data });
        }
    }
}
