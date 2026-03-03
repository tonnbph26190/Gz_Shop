using Microsoft.AspNetCore.Mvc;
using QuanApi.Services;
using BanQuanAu1.Web.Data;
using QuanApi.Data;

namespace QuanApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HoaTietController : ControllerBase
    {
        private readonly IHoaTietService _service;

        public HoaTietController(IHoaTietService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(string? keyword)
        {
            var result = await _service.GetAllAsync(keyword);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create(HoaTiet ht)
        {
            var success = await _service.CreateAsync(ht);
            return Ok(new { success });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] HoaTiet ht)
        {
            var success = await _service.UpdateAsync(id, ht);
            return Ok(new { success });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _service.DeleteAsync(id);
            return Ok(new { success });
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
        public async Task<IActionResult> GetPaged(int page = 1, int pageSize = 10,
            string? keyword = null, string? trangThai = null)
        {
            var (total, data) = await _service.GetPagedAsync(page, pageSize, keyword, trangThai);
            return Ok(new { total, data });
        }
    }
}
