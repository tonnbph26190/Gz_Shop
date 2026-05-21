using Microsoft.AspNetCore.Mvc;
using QuanApi.Repository.IRepository;
using QuanApi.Data;

namespace QuanApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GioHangsController : ControllerBase
    {
        private readonly GioHangIRepository _gioHangRepo;

        public GioHangsController(GioHangIRepository gioHangRepo)
        {
            _gioHangRepo = gioHangRepo;
        }

        // POST: api/GioHangs/add
        [HttpPost("add")]
        public IActionResult AddToGioHang(Guid iduser, Guid idsp, int soluong)
        {
            try
            {
                _gioHangRepo.AddGioHang(iduser, idsp, soluong);
                return Ok("Đã thêm vào giỏ hàng");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

		// GET: api/GioHangs/user/{userId}
		[HttpGet("user/{userId}")]
		public IActionResult GetByUser(Guid userId)
		{
			try
			{
				var gioHang = _gioHangRepo.GetByUserId(userId);

				if (gioHang == null)
				{
					return Ok(new GioHang
					{
						IDGioHang = Guid.NewGuid(),
						IDKhachHang = userId,
						ChiTietGioHangs = new List<ChiTietGioHang>()
					});
				}

				if (gioHang.ChiTietGioHangs == null)
				{
					gioHang.ChiTietGioHangs = new List<ChiTietGioHang>();
				}

				return Ok(gioHang);
			}
			catch
			{
				return Ok(new GioHang
				{
					IDGioHang = Guid.NewGuid(),
					IDKhachHang = userId,
					ChiTietGioHangs = new List<ChiTietGioHang>()
				});
			}
		}

		// DELETE: api/GioHangs/item/{id}
		[HttpDelete("item/{idgiohang}")]
        public IActionResult XoaChiTiet(Guid idgiohang)
        {
            _gioHangRepo.XoaChiTietGioHang(idgiohang);
            return Ok("Đã xóa sản phẩm khỏi giỏ hàng");
        }

        // PUT: api/GioHangs/item/{idghct}
        [HttpPut("item/{idghct}")]
        public IActionResult UpdateChiTiet(Guid idghct, [FromQuery] int soluong)
        {
            try
            {
                _gioHangRepo.UpdateChiTietGioHang(idghct, soluong);
                return Ok("Cập nhật giỏ hàng thành công");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
