using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using QuanApi.Services;

namespace QuanApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KhachHangController : ControllerBase
    {
        private readonly IKhachHangService _service;

        public KhachHangController(IKhachHangService service)
        {
            _service = service;
        }

        // GET: api/KhachHang?keyword=
        [HttpGet]
        public async Task<IActionResult> GetAll(string? keyword)
            => Ok(await _service.GetAllAsync(keyword));

        // GET: api/KhachHang/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        // POST: api/KhachHang/Create
        [HttpPost("Create")]
        public async Task<IActionResult> Create(KhachHang kh)
        {
            var success = await _service.CreateAsync(kh);
            return success ? Ok(new { success = true }) : BadRequest();
        }

        // PUT: api/KhachHang/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] KhachHang kh)
        {
            var success = await _service.UpdateAsync(id, kh);
            return success ? Ok(new { success = true }) : NotFound();
        }

        // DELETE: api/KhachHang/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _service.DeleteAsync(id);
            return success ? Ok(new { success = true }) : NotFound();
        }

        // PUT: api/KhachHang/ToggleStatus/{id}
        [HttpPut("ToggleStatus/{id}")]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var success = await _service.ToggleStatusAsync(id);
            return success ? Ok(new { success = true }) : NotFound();
        }

        // GET: api/KhachHang/paged
        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(
            int page = 1,
            int pageSize = 10,
            string? keyword = null,
            string? trangThai = null)
        {
            var (total, data) = await _service.GetPagedAsync(
                page, pageSize, keyword, trangThai);

            return Ok(new { total, data });
        }
    }
}