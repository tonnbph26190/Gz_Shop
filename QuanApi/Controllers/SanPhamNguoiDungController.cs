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

        // GET: api/SanPhamNguoiDungs?pageNumber=1&pageSize=10
        [HttpGet]
        public async Task<IActionResult> GetSanPhamChiTiets(
            int pageNumber = 1, int pageSize = 10,
            string? search = null, int? priceFrom = null, int? priceTo = null,
            string? category = null, string? size = null, string? color = null)
        {
            try
            {
                var result = await _service.GetSanPhamChiTietsAsync(
                    pageNumber, pageSize,
                    search, priceFrom, priceTo,
                    category, size, color);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi server: {ex.Message}" });
            }
        }
        // GET: api/SanPhamNguoiDungs/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetail(Guid id)
        {
            var result = await _service.GetDetailAsync(id);

            if (result == null)
                return NotFound(new { message = "Không tìm thấy sản phẩm." });

            return Ok(result);
        }

        // GET: api/SanPhamNguoiDungs/filter-options
        [HttpGet("filter-options")]
        public async Task<IActionResult> GetFilterOptions()
        {
            try
            {
                var filterOptions = await _service.GetFilterOptionsAsync();
                return Ok(filterOptions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi server: {ex.Message}" });
            }
        }
    }

}
