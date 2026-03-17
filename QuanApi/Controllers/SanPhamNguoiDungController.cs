using Microsoft.AspNetCore.Mvc;
using QuanApi.Repository.IRepository;
using QuanApi.Dtos;

namespace QuanApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SanPhamNguoiDungsController : ControllerBase
    {
        private readonly GioHangIRepository _gioHangRepo;

        public SanPhamNguoiDungsController(GioHangIRepository gioHangRepo)
        {
            _gioHangRepo = gioHangRepo;
        }

        // GET: api/SanPhamNguoiDungs?pageNumber=1&pageSize=10
        [HttpGet]
        public IActionResult GetSanPhamChiTiets(
            int pageNumber = 1, int pageSize = 10,
            string search = null, int? priceFrom = null, int? priceTo = null,
            string category = null, string size = null, string color = null)
        {
            try
            {
                var query = _gioHangRepo.ListSPCT(pageNumber, pageSize);

                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.TenSanPham.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

                if (priceFrom.HasValue)
                    query = query.Where(x => x.BienThes.Any(b => b.GiaSauGiam >= priceFrom.Value)).ToList();

                if (priceTo.HasValue)
                    query = query.Where(x => x.BienThes.Any(b => b.GiaSauGiam <= priceTo.Value)).ToList();

                if (!string.IsNullOrEmpty(category))
                    query = query.Where(x => x.DanhMuc.Contains(category, StringComparison.OrdinalIgnoreCase)).ToList();

                if (!string.IsNullOrEmpty(size))
                    query = query.Where(x => x.BienThes.Any(b => b.Size.Contains(size, StringComparison.OrdinalIgnoreCase))).ToList();

                if (!string.IsNullOrEmpty(color))
                    query = query.Where(x => x.BienThes.Any(b => b.Mau.Contains(color, StringComparison.OrdinalIgnoreCase))).ToList();

                // Đảm bảo mỗi sản phẩm có ảnh
                foreach (var sp in query)
                {
                    if (string.IsNullOrEmpty(sp.UrlAnh))
                    {
                        sp.UrlAnh = "/img/default-product.jpg";
                    }
                }

                return Ok(query);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi server: {ex.Message}" });
            }
        }

        [HttpGet("{id}")]
        public IActionResult GetDetail(Guid id)
        {
            var result = _gioHangRepo.detailSpct(id);
            if (result == null)
                return NotFound(new { message = "Không tìm thấy sản phẩm." });

            return Ok(result);
        }

        // GET: api/SanPhamNguoiDungs/filter-options
        [HttpGet("filter-options")]
        public IActionResult GetFilterOptions()
        {
            try
            {
                var filterOptions = _gioHangRepo.GetFilterOptions();
                return Ok(filterOptions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi server: {ex.Message}" });
            }
        }

    }
}
