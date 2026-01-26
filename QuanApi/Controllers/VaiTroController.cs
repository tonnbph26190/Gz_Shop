using Microsoft.AspNetCore.Mvc;
using QuanApi.Data;
using QuanApi.Services;

namespace QuanApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VaiTroController : ControllerBase
    {
        private readonly IVaiTroService _service;

        public VaiTroController(IVaiTroService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<VaiTro>>> GetVaiTro()
        {
            return Ok(await _service.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<VaiTro>> GetVaiTro(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutVaiTro(Guid id, VaiTro vaiTro)
        {
            var success = await _service.UpdateAsync(id, vaiTro);
            if (!success) return BadRequest("ID mismatch or record not found.");
            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<VaiTro>> PostVaiTro(VaiTro vaiTro)
        {
            var result = await _service.CreateAsync(vaiTro);
            return CreatedAtAction(nameof(GetVaiTro), new { id = result.IDVaiTro }, result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVaiTro(Guid id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
}
