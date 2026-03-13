using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;
using QuanApi.Services;

namespace QuanApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KhachHangPhieuGiamController : ControllerBase
    {
        private readonly IKhachHangPhieuGiamService _service;

        public KhachHangPhieuGiamController(IKhachHangPhieuGiamService service)
        {
            _service = service;
        }

        // GET: api/KhachHangPhieuGiam
        [HttpGet]
        public async Task<ActionResult<IEnumerable<KhachHangPhieuGiam>>> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        // GET: api/KhachHangPhieuGiam/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<KhachHangPhieuGiam>> GetById(Guid id)
        {
            var item = await _service.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }
        [HttpGet("by-voucher/{idPhieu:guid}")]
        public async Task<ActionResult<Guid?>> GetKhachHangByVoucher(Guid idPhieu)
        {
            var khachHangId = await _service.GetKhachHangByVoucherAsync(idPhieu);
            return Ok(khachHangId);
        }

    
        [HttpGet("phieu-giam-gia-cong-khai")]
        public async Task<ActionResult<IEnumerable<object>>> GetPublicDiscountVouchers()
        {
            try
            {
                var vouchers = await _service.GetPublicDiscountVouchersAsync();
                return Ok(vouchers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi lấy danh sách phiếu giảm giá công khai: {ex.Message}");
            }
        }
        [HttpGet("phieu-giam-gia-cua-khach-hang/{customerId:guid}")]
        public async Task<ActionResult<IEnumerable<object>>> GetCustomerDiscountVouchers(Guid customerId)
        {
            try
            {
                var vouchers = await _service.GetCustomerDiscountVouchersAsync(customerId);
                return Ok(vouchers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi lấy danh sách phiếu giảm giá của khách hàng: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create(KhachHangPhieuGiam model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.CreateAsync(model);

            return CreatedAtAction(nameof(GetById),
                new { id = result.IDKhachHangPhieuGiam },
                result);
        }
        // PUT: api/KhachHangPhieuGiam/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, KhachHangPhieuGiam model)
        {
            if (id != model.IDKhachHangPhieuGiam)
                return BadRequest("ID không khớp.");

            try
            {
                var success = await _service.UpdateAsync(id, model);
                if (!success) return NotFound();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict("Dữ liệu đã bị thay đổi bởi tiến trình khác.");
            }
        }

        // DELETE: api/KhachHangPhieuGiam/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound();

            return NoContent();
        }
    }

}
