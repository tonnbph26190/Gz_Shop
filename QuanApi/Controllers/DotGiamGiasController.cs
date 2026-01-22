using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using QuanApi.Dtos;
using QuanApi.Services;

namespace QuanApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DotGiamGiasController : ControllerBase
    {
        private readonly IDotGiamGiaService _service;
        private readonly ILogger<DotGiamGiasController> _logger;

        public DotGiamGiasController(
            IDotGiamGiaService service,
            ILogger<DotGiamGiasController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // GET: api/DotGiamGias/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var dot = await _service.GetByIdAsync(id);

            if (dot == null)
            {
                _logger.LogWarning("Không tìm thấy đợt giảm giá ID: {Id}", id);
                return NotFound();
            }

            return Ok(dot);
        }

        // POST: api/DotGiamGias
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DotGiamGiaCreateDto dto)
        {
            if (dto == null || dto.Dot == null)
                return BadRequest("Dữ liệu không hợp lệ");

            var success = await _service.CreateAsync(dto);

            if (success)
            {
                return Ok(new
                {
                    Success = true,
                    Message = "Đợt giảm giá đã được xử lý thành công. Nếu sản phẩm đã có đợt giảm giá đang hoạt động, đã được cập nhật thay vì tạo mới."
                });
            }

            return BadRequest("Tạo thất bại (có thể do ngày không hợp lệ)");
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] DotGiamGiaUpdateDto dto)
        {
            if (dto == null)
                return BadRequest("Dữ liệu không hợp lệ");

            var success = await _service.UpdateAsync(id, dto);

            if (!success)
                return BadRequest("Cập nhật thất bại");

            return Ok(new
            {
                Success = true,
                Message = "Cập nhật đợt giảm giá thành công"
            });
        }

        // GET: api/DotGiamGias/{id}/SanPhams
        [HttpGet("{id}/SanPhams")]
        public async Task<IActionResult> GetSanPhamsCuaDot(Guid id)
        {
            var sanPhams = await _service.GetSanPhamsCuaDotAsync(id);
            return Ok(sanPhams);
        }
        // GET: api/DotGiamGias/CheckActiveDiscounts
        [HttpGet("CheckActiveDiscounts")]
        public async Task<IActionResult> CheckActiveDiscounts([FromQuery] List<Guid> productIds)
        {
            if (productIds == null || !productIds.Any())
                return BadRequest("Danh sách sản phẩm không hợp lệ");

            var (hasActive, productIdsWithActive) =
                await _service.CheckActiveDiscountsAsync(productIds);

            return Ok(new
            {
                HasActiveDiscounts = hasActive,
                ProductIdsWithActiveDiscounts = productIdsWithActive,
                Message = hasActive
                    ? $"Có {productIdsWithActive.Count} sản phẩm đã có đợt giảm giá đang hoạt động. Đợt giảm giá hiện tại sẽ được cập nhật thay vì tạo mới."
                    : "Không có sản phẩm nào có đợt giảm giá đang hoạt động."
            });
        }


        // DELETE: api/DotGiamGias/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _service.DeleteAsync(id);
            return success ? Ok(new { Success = true }) : NotFound();
        }


        // PUT: api/DotGiamGias/UpdateTrangThai?id=GUID&trangThai=true
        [HttpPut("UpdateTrangThai")]
        public async Task<IActionResult> UpdateTrangThai([FromQuery] Guid id, [FromQuery] bool trangThai)
        {
            var success = await _service.UpdateTrangThaiAsync(id, trangThai);
            return success ? Ok(new { Success = true }) : NotFound();
        }

    }


}
