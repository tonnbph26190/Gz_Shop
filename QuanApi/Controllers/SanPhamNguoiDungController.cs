using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using QuanApi.Services;

namespace QuanApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SanPhamNguoiDungController : ControllerBase
    {
        private readonly ISanPhamNguoiDungService _service;

        public SanPhamNguoiDungController(ISanPhamNguoiDungService service)
        {
            _service = service;
        }

        // GET: api/SanPhamNguoiDung?keyword=
        [HttpGet]
        public async Task<IActionResult> GetAll(string? keyword)
        {
            var data = await _service.GetAllAsync(keyword);
            return Ok(data);
        }

        // GET: api/SanPhamNguoiDung/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        // PUT: api/SanPhamNguoiDung/ToggleStatus/{id}
        [HttpPut("ToggleStatus/{id}")]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var success = await _service.ToggleStatusAsync(id);
            return success ? Ok(new { success = true }) : NotFound();
        }

        // GET: api/SanPhamNguoiDung/paged
        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(
            int page = 1,
            int pageSize = 10,
            string? keyword = null,
            decimal? giaTu = null,
            decimal? giaDen = null)
        {
            var (total, data) = await _service.GetPagedAsync(
                page,
                pageSize,
                keyword,
                giaTu,
                giaDen);

            return Ok(new { total, data });
        }
    }
}
