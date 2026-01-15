using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using QuanApi.Services;

namespace QuanApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhuongThucThanhToansController : ControllerBase
    {
        private readonly IPhuongThucThanhToanService _service;

        public PhuongThucThanhToansController(IPhuongThucThanhToanService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(PhuongThucThanhToan entity)
        {
            return Ok(await _service.CreateAsync(entity));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, PhuongThucThanhToan entity)
        {
            var result = await _service.UpdateAsync(id, entity);
            return result ? Ok("Cập nhật thành công") : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _service.DeleteAsync(id);
            return result ? Ok("Xóa thành công") : NotFound();
        }
    }
}
