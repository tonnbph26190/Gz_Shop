using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;
using BanQuanAu1.Web.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using AutoMapper;
using QuanApi.Dtos;

namespace QuanApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PhieuGiamGiasController : ControllerBase
    {
        private readonly BanQuanAu1DbContext _context;

        public PhieuGiamGiasController(BanQuanAu1DbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PhieuGiamGia>>> GetAll()
        {
            return await _context.PhieuGiamGias
                                 .AsNoTracking()
                                 .OrderByDescending(p => p.NgayTao)
                                 .ToListAsync();
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<PhieuGiamGia>> GetById(Guid id)
        {
            var entity = await _context.PhieuGiamGias.FindAsync(id);
            return entity is null ? NotFound() : entity;
        }
        [HttpPost]
        public async Task<ActionResult<PhieuGiamGiaDto>> Create(
                [FromBody] CreatePhieuGiamGiaDto dto,
                [FromServices] IMapper mapper)
        {
            var model = mapper.Map<PhieuGiamGia>(dto);
            model.IDPhieuGiamGia = Guid.NewGuid();
            model.NgayTao = DateTime.UtcNow;
            model.LaCongKhai = true; // Mặc định là công khai
            model.SoLuong = 1; // Mỗi khách hàng 1 phiếu

            _context.PhieuGiamGias.Add(model);
            await _context.SaveChangesAsync();

            // Phân phối phiếu giảm giá cho tất cả khách hàng (mỗi người 1 phiếu)
            var allCustomers = await _context.KhachHang
                .Where(kh => kh.TrangThai)
                .ToListAsync();

            var khachHangPhieuGiamList = new List<KhachHangPhieuGiam>();

            foreach (var customer in allCustomers)
            {
                var khachHangPhieuGiam = new KhachHangPhieuGiam
                {
                    IDKhachHangPhieuGiam = Guid.NewGuid(),
                    MaKhachHangPhieuGiam = $"KHPG_{DateTime.Now:yyyyMMddHHmmss}_{customer.IDKhachHang.ToString().Substring(0, 8)}",
                    IDKhachHang = customer.IDKhachHang,
                    IDPhieuGiamGia = model.IDPhieuGiamGia,
                    SoLuong = 1, // Mỗi khách hàng chỉ được 1 phiếu
                    SoLuongDaSuDung = 0,
                    NgayTao = DateTime.UtcNow,
                    NguoiTao = User.Identity?.Name ?? "System",
                    LanCapNhatCuoi = null,
                    NguoiCapNhat = null,
                    TrangThai = true
                };

                khachHangPhieuGiamList.Add(khachHangPhieuGiam);
            }

            if (khachHangPhieuGiamList.Any())
            {
                _context.KhachHangPhieuGiams.AddRange(khachHangPhieuGiamList);
                await _context.SaveChangesAsync();
            }

            return CreatedAtAction(nameof(GetById),
                new { id = model.IDPhieuGiamGia },
                mapper.Map<PhieuGiamGiaDto>(model));
        }

        public class UpdatePayload
        {
            public PhieuGiamGia Phieu { get; set; }
            public Guid? KhachHangId { get; set; }
        }


        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePayload payload)
        {
            var model = payload.Phieu;

            if (id != model.IDPhieuGiamGia)
                return BadRequest("ID không khớp.");

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            // Luôn set là công khai và mỗi khách hàng 1 phiếu
            model.LaCongKhai = true;
            model.SoLuong = 1;

            _context.Entry(model).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();

                // Cập nhật phân phối phiếu giảm giá cho tất cả khách hàng
                var allCustomers = await _context.KhachHang
                    .Where(kh => kh.TrangThai)
                    .ToListAsync();

                // Xóa tất cả liên kết hiện tại
                var existingLinks = await _context.KhachHangPhieuGiams
                    .Where(x => x.IDPhieuGiamGia == id)
                    .ToListAsync();

                if (existingLinks.Any())
                {
                    _context.KhachHangPhieuGiams.RemoveRange(existingLinks);
                    await _context.SaveChangesAsync();
                }

                // Tạo liên kết mới cho tất cả khách hàng
                var khachHangPhieuGiamList = new List<KhachHangPhieuGiam>();

                foreach (var customer in allCustomers)
                {
                    var khachHangPhieuGiam = new KhachHangPhieuGiam
                    {
                        IDKhachHangPhieuGiam = Guid.NewGuid(),
                        MaKhachHangPhieuGiam = $"KHPG_{DateTime.Now:yyyyMMddHHmmss}_{customer.IDKhachHang.ToString().Substring(0, 8)}",
                        IDKhachHang = customer.IDKhachHang,
                        IDPhieuGiamGia = id,
                        SoLuong = 1, // Mỗi khách hàng chỉ được 1 phiếu
                        SoLuongDaSuDung = 0,
                        NgayTao = DateTime.UtcNow,
                        NguoiTao = User.Identity?.Name ?? "System",
                        LanCapNhatCuoi = null,
                        NguoiCapNhat = null,
                        TrangThai = true
                    };

                    khachHangPhieuGiamList.Add(khachHangPhieuGiam);
                }

                if (khachHangPhieuGiamList.Any())
                {
                    _context.KhachHangPhieuGiams.AddRange(khachHangPhieuGiamList);
                    await _context.SaveChangesAsync();
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.PhieuGiamGias.Any(x => x.IDPhieuGiamGia == id))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var entity = await _context.PhieuGiamGias.FindAsync(id);
            if (entity == null)
                return NotFound();

            // Xóa tất cả liên kết với khách hàng trước
            var customerLinks = await _context.KhachHangPhieuGiams
                .Where(x => x.IDPhieuGiamGia == id)
                .ToListAsync();

            if (customerLinks.Any())
            {
                _context.KhachHangPhieuGiams.RemoveRange(customerLinks);
            }

            // Sau đó xóa phiếu giảm giá
            _context.PhieuGiamGias.Remove(entity);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Xóa liên kết giữa khách hàng và phiếu giảm giá
        [HttpDelete("remove-customer/{id:guid}")]
        public async Task<IActionResult> RemoveCustomerFromVoucher(Guid id, [FromQuery] Guid customerId)
        {
            var link = await _context.KhachHangPhieuGiams
                .FirstOrDefaultAsync(x => x.IDPhieuGiamGia == id && x.IDKhachHang == customerId);

            if (link == null)
                return NotFound("Không tìm thấy liên kết giữa khách hàng và phiếu giảm giá.");

            _context.KhachHangPhieuGiams.Remove(link);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Thêm khách hàng vào phiếu giảm giá
        [HttpPost("add-customer/{id:guid}")]
        public async Task<IActionResult> AddCustomerToVoucher(Guid id, [FromQuery] Guid customerId)
        {
            // Kiểm tra phiếu giảm giá có tồn tại không
            var voucher = await _context.PhieuGiamGias.FindAsync(id);
            if (voucher == null)
                return NotFound("Không tìm thấy phiếu giảm giá.");

            // Kiểm tra khách hàng có tồn tại không
            var customer = await _context.KhachHang.FindAsync(customerId);
            if (customer == null)
                return NotFound("Không tìm thấy khách hàng.");

            // Kiểm tra liên kết đã tồn tại chưa
            var existingLink = await _context.KhachHangPhieuGiams
                .FirstOrDefaultAsync(x => x.IDPhieuGiamGia == id && x.IDKhachHang == customerId);

            if (existingLink != null)
                return BadRequest("Khách hàng đã được thêm vào phiếu giảm giá này.");

            // Tạo liên kết mới
            var khachHangPhieuGiam = new KhachHangPhieuGiam
            {
                IDKhachHangPhieuGiam = Guid.NewGuid(),
                MaKhachHangPhieuGiam = $"KHPG_{DateTime.Now:yyyyMMddHHmmss}_{customerId.ToString().Substring(0, 8)}",
                IDKhachHang = customerId,
                IDPhieuGiamGia = id,
                SoLuong = voucher.SoLuong,
                SoLuongDaSuDung = 0,
                NgayTao = DateTime.UtcNow,
                NguoiTao = User.Identity?.Name ?? "System",
                LanCapNhatCuoi = null,
                NguoiCapNhat = null,
                TrangThai = true
            };

            _context.KhachHangPhieuGiams.Add(khachHangPhieuGiam);
            await _context.SaveChangesAsync();

            return Ok("Đã thêm khách hàng vào phiếu giảm giá thành công.");
        }

        // Lấy danh sách khách hàng của một phiếu giảm giá
        [HttpGet("customers/{id:guid}")]
        public async Task<IActionResult> GetVoucherCustomers(Guid id)
        {
            var voucher = await _context.PhieuGiamGias.FindAsync(id);
            if (voucher == null)
                return NotFound("Không tìm thấy phiếu giảm giá.");

            if (voucher.LaCongKhai)
            {
                // Nếu là phiếu công khai, trả về tất cả khách hàng đã nhận phiếu
                var customers = await _context.KhachHangPhieuGiams
                    .Include(x => x.KhachHang)
                    .Where(x => x.IDPhieuGiamGia == id && x.TrangThai)
                    .Select(x => new
                    {
                        id = x.IDKhachHang,
                        maKhachHang = x.KhachHang.MaKhachHang,
                        tenKhachHang = x.KhachHang.TenKhachHang,
                        email = x.KhachHang.Email,
                        soDienThoai = x.KhachHang.SoDienThoai,
                        soLuong = x.SoLuong,
                        soLuongDaSuDung = x.SoLuongDaSuDung,
                        soLuongConLai = x.SoLuong - x.SoLuongDaSuDung,
                        ngayTao = x.NgayTao,
                        nguoiTao = x.NguoiTao,
                        trangThai = x.TrangThai
                    })
                    .ToListAsync();

                return Ok(new
                {
                    isPublic = true,
                    totalCustomers = customers.Count,
                    customers = customers
                });
            }
            else
            {
                // Nếu không phải công khai, chỉ trả về khách hàng được chỉ định
                var customers = await _context.KhachHangPhieuGiams
                    .Include(x => x.KhachHang)
                    .Where(x => x.IDPhieuGiamGia == id && x.TrangThai)
                    .Select(x => new
                    {
                        id = x.IDKhachHang,
                        maKhachHang = x.KhachHang.MaKhachHang,
                        tenKhachHang = x.KhachHang.TenKhachHang,
                        email = x.KhachHang.Email,
                        soDienThoai = x.KhachHang.SoDienThoai,
                        soLuong = x.SoLuong,
                        soLuongDaSuDung = x.SoLuongDaSuDung,
                        soLuongConLai = x.SoLuong - x.SoLuongDaSuDung,
                        ngayTao = x.NgayTao,
                        nguoiTao = x.NguoiTao,
                        trangThai = x.TrangThai
                    })
                    .ToListAsync();

                return Ok(new
                {
                    isPublic = false,
                    totalCustomers = customers.Count,
                    customers = customers
                });
            }
        }



        // Sử dụng phiếu giảm giá (tăng số lượng đã sử dụng)
        [HttpPost("use-voucher/{id:guid}")]
        public async Task<IActionResult> UseVoucher(Guid id, [FromQuery] Guid customerId)
        {
            var customerVoucher = await _context.KhachHangPhieuGiams
                .Include(x => x.KhachHang)
                .Include(x => x.PhieuGiamGia)
                .FirstOrDefaultAsync(x => x.IDPhieuGiamGia == id && x.IDKhachHang == customerId);

            if (customerVoucher == null)
                return NotFound("Không tìm thấy phiếu giảm giá cho khách hàng này.");

            if (!customerVoucher.TrangThai)
                return BadRequest("Phiếu giảm giá này đã bị vô hiệu hóa.");

            if (customerVoucher.SoLuongDaSuDung >= customerVoucher.SoLuong)
                return BadRequest("Phiếu giảm giá này đã được sử dụng hết.");

            // Kiểm tra thời gian hiệu lực
            var now = DateTime.UtcNow;
            if (now < customerVoucher.PhieuGiamGia.NgayBatDau || now > customerVoucher.PhieuGiamGia.NgayKetThuc)
                return BadRequest("Phiếu giảm giá này không còn hiệu lực.");

            // Tăng số lượng đã sử dụng
            customerVoucher.SoLuongDaSuDung++;
            customerVoucher.LanCapNhatCuoi = now;
            customerVoucher.NguoiCapNhat = User.Identity?.Name ?? "System";

            // Nếu đã sử dụng hết, vô hiệu hóa
            if (customerVoucher.SoLuongDaSuDung >= customerVoucher.SoLuong)
            {
                customerVoucher.TrangThai = false;
            }

            await _context.SaveChangesAsync();

            var message = customerVoucher.PhieuGiamGia.LaCongKhai 
                ? "Sử dụng phiếu giảm giá công khai thành công." 
                : "Sử dụng phiếu giảm giá thành công.";

            return Ok(new
            {
                message = message,
                soLuongConLai = customerVoucher.SoLuong - customerVoucher.SoLuongDaSuDung,
                daSuDungHet = customerVoucher.SoLuongDaSuDung >= customerVoucher.SoLuong,
                isPublic = customerVoucher.PhieuGiamGia.LaCongKhai
            });
        }

        // Reset số lượng đã sử dụng (cho admin)
        [HttpPost("reset-usage/{id:guid}")]
        public async Task<IActionResult> ResetVoucherUsage(Guid id, [FromQuery] Guid customerId)
        {
            var customerVoucher = await _context.KhachHangPhieuGiams
                .FirstOrDefaultAsync(x => x.IDPhieuGiamGia == id && x.IDKhachHang == customerId);

            if (customerVoucher == null)
                return NotFound("Không tìm thấy phiếu giảm giá cho khách hàng này.");

            customerVoucher.SoLuongDaSuDung = 0;
            customerVoucher.TrangThai = true;
            customerVoucher.LanCapNhatCuoi = DateTime.UtcNow;
            customerVoucher.NguoiCapNhat = User.Identity?.Name ?? "System";

            await _context.SaveChangesAsync();

            return Ok("Đã reset số lượng sử dụng phiếu giảm giá.");
        }

        // Kiểm tra mã giảm giá


        [HttpGet("kiem-tra")]
        public async Task<IActionResult> KiemTraMaGiamGia([FromQuery] string code)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                    return BadRequest("Mã giảm giá không được để trống.");

                var phieuGiamGia = await _context.PhieuGiamGias
                    .FirstOrDefaultAsync(pgg => pgg.MaCode == code && pgg.TrangThai);

                if (phieuGiamGia == null)
                    return Ok(new { success = false, message = "Mã giảm giá không tồn tại hoặc đã bị vô hiệu hóa." });

                // Kiểm tra thời gian hiệu lực
                if (phieuGiamGia.NgayBatDau > DateTime.Now)
                    return Ok(new { success = false, message = "Mã giảm giá chưa có hiệu lực." });

                if (phieuGiamGia.NgayKetThuc < DateTime.Now)
                    return Ok(new { success = false, message = "Mã giảm giá đã hết hạn." });

                // Tính toán giá trị giảm giá
                decimal tienGiam = phieuGiamGia.GiaTriGiam;
                if (phieuGiamGia.GiaTriGiamToiDa.HasValue && tienGiam > phieuGiamGia.GiaTriGiamToiDa.Value)
                {
                    tienGiam = phieuGiamGia.GiaTriGiamToiDa.Value;
                }

                return Ok(new { 
                    success = true, 
                    message = "Mã giảm giá hợp lệ.",
                    tienGiam = tienGiam,
                    phanTramGiam = phieuGiamGia.GiaTriGiam,
                    giaTriToiDa = phieuGiamGia.GiaTriGiamToiDa,
                    donToiThieu = phieuGiamGia.DonToiThieu
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }
    }
}
