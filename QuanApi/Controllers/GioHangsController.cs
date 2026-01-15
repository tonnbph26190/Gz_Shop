using Microsoft.AspNetCore.Mvc;
using QuanApi.Services;

namespace QuanApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GioHangsController : ControllerBase
    {
        private readonly IGioHangService _gioHangService;

        public GioHangsController(IGioHangService gioHangService)
        {
            _gioHangService = gioHangService;
        }

        // POST: api/GioHangs/add
        [HttpPost("add")]
        public async Task<IActionResult> AddToGioHang(Guid iduser, Guid idsp, int soluong)
        {
            var result = await _gioHangService.AddToGioHangAsync(iduser, idsp, soluong);
            return result ? Ok("Đã thêm vào giỏ hàng") : BadRequest();
        }

        // GET: api/GioHangs/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUser(Guid userId)
        {
            var gioHang = await _gioHangService.GetByUserIdAsync(userId);
            return Ok(gioHang);
        }

        // DELETE: api/GioHangs/item/{id}
        [HttpDelete("item/{idgiohang}")]
        public async Task<IActionResult> XoaChiTiet(Guid idgiohang)
        {
            var result = await _gioHangService.XoaChiTietGioHangAsync(idgiohang);
            return result ? Ok("Đã xóa sản phẩm khỏi giỏ hàng") : NotFound();
        }

        // PUT: api/GioHangs/item/{idghct}
        [HttpPut("item/{idghct}")]
        public async Task<IActionResult> UpdateChiTiet(Guid idghct, [FromQuery] int soluong)
        {
            var result = await _gioHangService.UpdateChiTietGioHangAsync(idghct, soluong);
            return result ? Ok("Cập nhật giỏ hàng thành công") : NotFound();
        }
    }
}
