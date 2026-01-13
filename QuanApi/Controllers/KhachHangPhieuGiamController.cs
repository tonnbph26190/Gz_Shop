using Microsoft.AspNetCore.Mvc;
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

        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok(await _service.GetAllAsync());

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpGet("by-voucher/{idPhieu:guid}")]
        public async Task<IActionResult> GetKhachHangByVoucher(Guid idPhieu)
            => Ok(await _service.GetKhachHangByVoucherAsync(idPhieu));

        [HttpGet("phieu-giam-gia-cong-khai")]
        public async Task<IActionResult> GetPublicDiscountVouchers()
            => Ok(await _service.GetPublicDiscountVouchersAsync());

        [HttpGet("phieu-giam-gia-cua-khach-hang/{customerId:guid}")]
        public async Task<IActionResult> GetCustomerDiscountVouchers(Guid customerId)
            => Ok(await _service.GetCustomerDiscountVouchersAsync(customerId));

        [HttpPost]
        public async Task<IActionResult> Create(KhachHangPhieuGiam model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var success = await _service.CreateAsync(model);
            return success ? Ok(new { success = true }) : BadRequest();
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, KhachHangPhieuGiam model)
        {
            var success = await _service.UpdateAsync(id, model);
            return success ? Ok(new { success = true }) : NotFound();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _service.DeleteAsync(id);
            return success ? Ok(new { success = true }) : NotFound();
        }
    }
}
