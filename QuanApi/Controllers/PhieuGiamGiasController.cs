using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using QuanApi.Services;

namespace QuanApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PhieuGiamGiasController : ControllerBase
    {
        private readonly IPhieuGiamGiaService _service;

        public PhieuGiamGiasController(IPhieuGiamGiaService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok(await _service.GetAllAsync());

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(PhieuGiamGia model)
        {
            var success = await _service.CreateAsync(model, User.Identity?.Name);
            return success ? Ok(new { success = true }) : BadRequest();
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, PhieuGiamGia model)
        {
            var success = await _service.UpdateAsync(id, model, User.Identity?.Name);
            return success ? Ok(new { success = true }) : NotFound();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _service.DeleteAsync(id);
            return success ? Ok(new { success = true }) : NotFound();
        }

        [HttpPost("use-voucher/{id:guid}")]
        public async Task<IActionResult> UseVoucher(Guid id, [FromQuery] Guid customerId)
        {
            var success = await _service.UseVoucherAsync(id, customerId, User.Identity?.Name);
            return success ? Ok("Sử dụng phiếu thành công") : BadRequest();
        }

        [HttpGet("kiem-tra")]
        public async Task<IActionResult> KiemTraMa(string code)
        {
            var result = await _service.KiemTraMaAsync(code);
            return result == null ? BadRequest("Mã không hợp lệ") : Ok(result);
        }

        [HttpGet("customers/{id:guid}")]
        public async Task<IActionResult> Customers(Guid id)
            => Ok(await _service.GetVoucherCustomersAsync(id));
    }
}
