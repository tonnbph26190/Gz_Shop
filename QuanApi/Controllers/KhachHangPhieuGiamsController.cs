using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;
using BanQuanAu1.Web.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KhachHangPhieuGiamController : ControllerBase
    {
        private readonly BanQuanAu1DbContext _context;

        public KhachHangPhieuGiamController(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        // GET: api/KhachHangPhieuGiam
        [HttpGet]
        public async Task<ActionResult<IEnumerable<KhachHangPhieuGiam>>> GetAll()
        {
            return await _context.KhachHangPhieuGiams
                .AsNoTracking()
                .Include(k => k.KhachHang)
                .Include(k => k.PhieuGiamGia)
                .ToListAsync();
        }

        // GET: api/KhachHangPhieuGiam/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<KhachHangPhieuGiam>> GetById(Guid id)
        {
            var item = await _context.KhachHangPhieuGiams
                .Include(k => k.KhachHang)
                .Include(k => k.PhieuGiamGia)
                .FirstOrDefaultAsync(x => x.IDKhachHangPhieuGiam == id);

            return item == null ? NotFound() : item;
        }

        // GET: api/KhachHangPhieuGiam/by-voucher/{idPhieu}
        [HttpGet("by-voucher/{idPhieu:guid}")]
        public async Task<ActionResult<Guid?>> GetKhachHangByVoucher(Guid idPhieu)
        {
            var item = await _context.KhachHangPhieuGiams
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IDPhieuGiamGia == idPhieu);

            return item?.IDKhachHang;
        }

        // GET: api/KhachHangPhieuGiam/phieu-giam-gia-cong-khai
        [HttpGet("phieu-giam-gia-cong-khai")]
        public async Task<ActionResult<IEnumerable<object>>> GetPublicDiscountVouchers()
        {
            try
            {
                // Lấy tất cả phiếu giảm giá công khai đang hoạt động
                var publicVouchers = await _context.PhieuGiamGias
                    .Where(p => p.LaCongKhai == true && 
                               p.TrangThai && 
                               p.NgayBatDau <= DateTime.UtcNow && 
                               p.NgayKetThuc >= DateTime.UtcNow)
                    .Select(p => new
                    {
                        id = p.IDPhieuGiamGia,
                        maCode = p.MaCode,
                        tenPhieu = p.TenPhieu,
                        giaTriGiam = p.GiaTriGiam,
                        giaTriGiamToiDa = p.GiaTriGiamToiDa,
                        donToiThieu = p.DonToiThieu,
                        ngayBatDau = p.NgayBatDau,
                        ngayKetThuc = p.NgayKetThuc,
                        soLuong = p.SoLuong,
                        loaiPhieu = p.LaCongKhai ? "Công khai" : "Riêng tư",
                        trangThai = p.TrangThai
                    })
                    .ToListAsync();

                return Ok(publicVouchers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi lấy danh sách phiếu giảm giá công khai: {ex.Message}");
            }
        }

        // GET: api/KhachHangPhieuGiam/phieu-giam-gia-cua-khach-hang/{customerId}
        [HttpGet("phieu-giam-gia-cua-khach-hang/{customerId:guid}")]
        public async Task<ActionResult<IEnumerable<object>>> GetCustomerDiscountVouchers(Guid customerId)
        {
            try
            {
                var vouchers = await _context.KhachHangPhieuGiams
                    .Include(k => k.PhieuGiamGia)
                    .Where(x => x.IDKhachHang == customerId && 
                               x.TrangThai && 
                               x.PhieuGiamGia.TrangThai &&
                               x.SoLuongDaSuDung < x.SoLuong && // Chỉ lấy phiếu còn số lượng
                               x.PhieuGiamGia.NgayBatDau <= DateTime.UtcNow && // Kiểm tra thời gian hiệu lực
                               x.PhieuGiamGia.NgayKetThuc >= DateTime.UtcNow)
                    .Select(x => new {
                        id = x.IDPhieuGiamGia,
                        maCode = x.PhieuGiamGia.MaCode,
                        tenPhieu = x.PhieuGiamGia.TenPhieu,
                        giaTriGiam = x.PhieuGiamGia.GiaTriGiam,
                        giaTriGiamToiDa = x.PhieuGiamGia.GiaTriGiamToiDa,
                        donToiThieu = x.PhieuGiamGia.DonToiThieu,
                        ngayBatDau = x.PhieuGiamGia.NgayBatDau,
                        ngayKetThuc = x.PhieuGiamGia.NgayKetThuc,
                        soLuong = x.SoLuong,
                        soLuongDaSuDung = x.SoLuongDaSuDung,
                        soLuongConLai = x.SoLuong - x.SoLuongDaSuDung,
                        loaiPhieu = x.PhieuGiamGia.LaCongKhai ? "Công khai" : "Riêng tư"
                    })
                    .ToListAsync();

                return Ok(vouchers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi lấy danh sách phiếu giảm giá của khách hàng: {ex.Message}");
            }
        }

        // POST: api/KhachHangPhieuGiam
        [HttpPost]
        public async Task<IActionResult> Create(KhachHangPhieuGiam model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.KhachHangPhieuGiams.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = model.IDKhachHangPhieuGiam }, model);
        }

        // PUT: api/KhachHangPhieuGiam/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, KhachHangPhieuGiam model)
        {
            if (id != model.IDKhachHangPhieuGiam)
                return BadRequest("ID không khớp.");

            _context.Entry(model).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.KhachHangPhieuGiams.Any(e => e.IDKhachHangPhieuGiam == id))
                    return NotFound();
                throw;
            }
        }

        // DELETE: api/KhachHangPhieuGiam/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var entity = await _context.KhachHangPhieuGiams.FindAsync(id);
            if (entity == null) return NotFound();

            _context.KhachHangPhieuGiams.Remove(entity);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
