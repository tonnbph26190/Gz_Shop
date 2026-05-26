using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;
using QuanApi.Dtos;
using QuanApi.Services;
using static QuanApi.Services.PhieuGiamGiaService;

namespace QuanApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PhieuGiamGiaController : ControllerBase
    {
        private readonly IPhieuGiamGiaService _service;
        private readonly IMapper _mapper;

        public PhieuGiamGiaController(
            IPhieuGiamGiaService service,
            IMapper mapper)
        {
            _service = service;
            _mapper = mapper;
        }

        // GET: api/PhieuGiamGia
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PhieuGiamGia>>> GetAll()
        {
            var data = await _service.GetAllAsync();
            return Ok(data);
        }

        // GET: api/PhieuGiamGia/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<PhieuGiamGia>> GetById(Guid id)
        {
            var entity = await _service.GetByIdAsync(id);
            return entity is null ? NotFound() : Ok(entity);
        }

        // POST: api/PhieuGiamGia
        [HttpPost]
        public async Task<ActionResult<PhieuGiamGiaDto>> Create(
            [FromBody] CreatePhieuGiamGiaDto dto)
        {
            var nguoiTao = User.Identity?.Name ?? "System";

            var model = await _service.CreateAsync(dto, nguoiTao);

            return CreatedAtAction(
                nameof(GetById),
                new { id = model.IDPhieuGiamGia },
                _mapper.Map<PhieuGiamGiaDto>(model));
        }
        // PUT: api/PhieuGiamGia/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePayload payload)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            try
            {
                var nguoiCapNhat = User.Identity?.Name ?? "System";

                var success = await _service.UpdateAsync(id, payload, nguoiCapNhat);
                if (!success) return NotFound();

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict("Dữ liệu đã bị thay đổi bởi tiến trình khác.");
            }
        }

        // DELETE: api/PhieuGiamGia/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound();

            return NoContent();
        }
        // DELETE: api/PhieuGiamGia/remove-customer/{id}
        [HttpDelete("remove-customer/{id:guid}")]
        public async Task<IActionResult> RemoveCustomerFromVoucher(
            Guid id,
            [FromQuery] Guid customerId)
        {
            var success = await _service.RemoveCustomerAsync(id, customerId);
            if (!success)
                return NotFound("Không tìm thấy liên kết giữa khách hàng và phiếu giảm giá.");

            return NoContent();
        }

        // POST: api/PhieuGiamGia/add-customer/{id}
        [HttpPost("add-customer/{id:guid}")]
        public async Task<IActionResult> AddCustomerToVoucher(
            Guid id,
            [FromQuery] Guid customerId)
        {
            var nguoiTao = User.Identity?.Name ?? "System";

            var result = await _service.AddCustomerAsync(id, customerId, nguoiTao);

            if (!result.success)
                return BadRequest(result.message);

            return Ok(result.message);
        }

        // GET: api/PhieuGiamGia/customers/{id}
        [HttpGet("customers/{id:guid}")]
        public async Task<IActionResult> GetVoucherCustomers(Guid id)
        {
            var result = await _service.GetVoucherCustomersAsync(id);
            if (result == null)
                return NotFound("Không tìm thấy phiếu giảm giá.");

            return Ok(result);
        }
        // POST: api/PhieuGiamGia/use-voucher/{id}
        [HttpPost("use-voucher/{id:guid}")]
        public async Task<IActionResult> UseVoucher(
            Guid id,
            [FromQuery] Guid customerId)
        {
            var nguoiCapNhat = User.Identity?.Name ?? "System";

            var result = await _service.UseVoucherAsync(id, customerId, nguoiCapNhat);

            if (!result.success)
                return BadRequest(result.message);

            return Ok(new
            {
                message = result.message,
                result.data
            });
        }

        // POST: api/PhieuGiamGia/reset-usage/{id}
        [HttpPost("reset-usage/{id:guid}")]
        public async Task<IActionResult> ResetVoucherUsage(
            Guid id,
            [FromQuery] Guid customerId)
        {
            var nguoiCapNhat = User.Identity?.Name ?? "System";

            var success = await _service.ResetVoucherUsageAsync(
                id,
                customerId,
                nguoiCapNhat);

            if (!success)
                return NotFound("Không tìm thấy phiếu giảm giá cho khách hàng này.");

            return Ok("Đã reset số lượng sử dụng phiếu giảm giá.");
        }

        // GET: api/PhieuGiamGia/kiem-tra?code=ABC&tongTien=100000
        [HttpGet("kiem-tra")]
        public async Task<IActionResult> KiemTraMaGiamGia([FromQuery] string code, [FromQuery] decimal? tongTien = null)
        {
            var result = await _service.CheckVoucherCodeAsync(code, tongTien);
            return Ok(result);
        }

    }

}
