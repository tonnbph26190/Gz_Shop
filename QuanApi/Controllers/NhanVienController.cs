using Microsoft.AspNetCore.Mvc;
using QuanApi.Dtos;
using QuanApi.Services;
using System.Security.Claims;

namespace QuanApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NhanVienController : ControllerBase
    {
        private readonly INhanVienService _service;
        private readonly ILogger<NhanVienController> _logger;

        public NhanVienController(INhanVienService service, ILogger<NhanVienController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResultGeneric<NhanVienResponseDto>>> GetNhanViens([FromQuery] NhanVienFilterDto filter)
        {
            _logger.LogInformation("Retrieving employees with filter: {@Filter}", filter);
            var result = await _service.GetNhanViensAsync(filter);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<NhanVienResponseDto>> GetNhanVien(Guid id)
        {
            var result = await _service.GetNhanVienByIdAsync(id);
            if (result == null) return NotFound($"Employee with ID {id} not found.");
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<NhanVienResponseDto>> PostNhanVien([FromBody] NhanVienCreateDto dto)
        {
            try
            {
                var result = await _service.CreateNhanVienAsync(dto);
                return CreatedAtAction(nameof(GetNhanVien), new { id = result.IDNhanVien }, result);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutNhanVien(Guid id, [FromBody] NhanVienUpdateDto dto)
        {
            var currentUserId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            try
            {
                await _service.UpdateNhanVienAsync(id, dto, currentUserId);
                return NoContent();
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNhanVien(Guid id)
        {
            var deleted = await _service.DeleteNhanVienAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }

        [HttpGet("employee-role-stats")]
        public async Task<IActionResult> GetEmployeeRoleStats()
        {
            var stats = await _service.GetEmployeeRoleStatsAsync();
            return Ok(stats);
        }
    }
}