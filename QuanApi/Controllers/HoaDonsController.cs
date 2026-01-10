using BanQuanAu1.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;
using QuanApi.Dtos;
using QuanApi.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace QuanApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HoaDonsController : ControllerBase
    {
        private readonly BanQuanAu1DbContext _context;
        private readonly ILogger<HoaDonsController> _logger;
        private readonly IEmailService _emailService;
        private readonly IOrderHistoryService _orderHistoryService;

        public HoaDonsController(BanQuanAu1DbContext context, ILogger<HoaDonsController> logger, 
            IEmailService emailService, IOrderHistoryService orderHistoryService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _orderHistoryService = orderHistoryService;
        }

        // GET: api/HoaDons - Cho admin (hiển thị tất cả đơn hàng)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetHoaDons(
            int page = 1, 
            int pageSize = 10,
            string? trangThai = null,
            string? tuNgay = null,
            string? denNgay = null,
            string? loaiDonHang = null,
            string? khachHang = null,
            string? maDonHang = null)
        {
            try
            {
                var query = _context.HoaDons
                    .Include(h => h.KhachHang)
                    .Include(h => h.NhanVien)
                    .Include(h => h.PhieuGiamGia)
                    .Include(h => h.PhuongThucThanhToan)
                    .Include(h => h.ChiTietHoaDons) // Thêm include này để tránh lỗi
                    .AsQueryable();

                // Áp dụng các bộ lọc
                if (!string.IsNullOrEmpty(trangThai))
                {
                    query = query.Where(h => h.TrangThai == trangThai);
                }

                if (!string.IsNullOrEmpty(tuNgay) && DateTime.TryParse(tuNgay, out var tuNgayDate))
                {
                    query = query.Where(h => h.NgayTao.Date >= tuNgayDate.Date);
                }

                if (!string.IsNullOrEmpty(denNgay) && DateTime.TryParse(denNgay, out var denNgayDate))
                {
                    query = query.Where(h => h.NgayTao.Date <= denNgayDate.Date);
                }

                if (!string.IsNullOrEmpty(loaiDonHang))
                {
                    if (loaiDonHang == "online")
                    {
                        query = query.Where(h => !string.IsNullOrEmpty(h.DiaChiGiaoHang));
                    }
                    else if (loaiDonHang == "taiquay")
                    {
                        query = query.Where(h => string.IsNullOrEmpty(h.DiaChiGiaoHang));
                    }
                }

                if (!string.IsNullOrEmpty(khachHang))
                {
                    query = query.Where(h => 
                        (h.KhachHang != null && h.KhachHang.TenKhachHang.Contains(khachHang)) ||
                        (h.TenNguoiNhan != null && h.TenNguoiNhan.Contains(khachHang)) ||
                        (h.SoDienThoaiNguoiNhan != null && h.SoDienThoaiNguoiNhan.Contains(khachHang))
                    );
                }

                if (!string.IsNullOrEmpty(maDonHang))
                {
                    query = query.Where(h => h.MaHoaDon.Contains(maDonHang));
                }

                // Tính tổng số bản ghi sau khi lọc
                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                // Áp dụng phân trang
                var hoaDons = await query
                    .OrderByDescending(h => h.NgayTao)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(h => new
                    {
                        IDHoaDon = h.IDHoaDon,
                        MaHoaDon = h.MaHoaDon,
                        TongTien = h.TongTien,
                        TienGiam = h.TienGiam,
                        TrangThai = h.TrangThai,
                        NgayTao = h.NgayTao,
                        TenNguoiNhan = h.TenNguoiNhan,
                        SoDienThoaiNguoiNhan = h.SoDienThoaiNguoiNhan,
                        DiaChiGiaoHang = h.DiaChiGiaoHang,
                        KhachHang = h.KhachHang != null ? new
                        {
                            IDKhachHang = h.KhachHang.IDKhachHang,
                            TenKhachHang = h.KhachHang.TenKhachHang,
                            SoDienThoai = h.KhachHang.SoDienThoai
                        } : null,
                        NhanVien = h.NhanVien != null ? new
                        {
                            IDNhanVien = h.NhanVien.IDNhanVien,
                            TenNhanVien = h.NhanVien.TenNhanVien
                        } : null,
                        SoLuongSanPham = h.ChiTietHoaDons.Count
                    })
                    .ToListAsync();

                // Thêm thông tin phân trang vào response headers
                Response.Headers.Append("X-Total-Count", totalCount.ToString());
                Response.Headers.Append("X-Total-Pages", totalPages.ToString());
                Response.Headers.Append("X-Current-Page", page.ToString());
                Response.Headers.Append("X-Page-Size", pageSize.ToString());

                return Ok(new
                {
                    Data = hoaDons,
                    Pagination = new
                    {
                        TotalCount = totalCount,
                        TotalPages = totalPages,
                        CurrentPage = page,
                        PageSize = pageSize,
                        HasPreviousPage = page > 1,
                        HasNextPage = page < totalPages
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách hóa đơn: {Message}", ex.Message);
                return StatusCode(500, "Lỗi server khi lấy danh sách hóa đơn");
            }
        }

        // GET: api/HoaDons/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetHoaDon(Guid id)
        {
            try
            {
            var hoaDon = await _context.HoaDons
                .Include(h => h.KhachHang)
                .Include(h => h.NhanVien)
                .Include(h => h.PhieuGiamGia)
                .Include(h => h.PhuongThucThanhToan)
                .Include(h => h.ChiTietHoaDons)
                    .ThenInclude(ct => ct.SanPhamChiTiet)
                        .ThenInclude(spct => spct.KichCo)
                .Include(h => h.ChiTietHoaDons)
                    .ThenInclude(ct => ct.SanPhamChiTiet)
                        .ThenInclude(spct => spct.MauSac)
                .Include(h => h.ChiTietHoaDons)
                    .ThenInclude(ct => ct.SanPhamChiTiet)
                        .ThenInclude(spct => spct.HoaTiet) // Thêm include cho HoaTiet
                .Include(h => h.ChiTietHoaDons)
                    .ThenInclude(ct => ct.SanPhamChiTiet)
                        .ThenInclude(spct => spct.SanPham) // Thêm include này để tránh lỗi
                    .Where(h => h.IDHoaDon == id)
                    .Select(h => new
                    {
                        IDHoaDon = h.IDHoaDon,
                        MaHoaDon = h.MaHoaDon,
                        TongTien = h.TongTien,
                        TienGiam = h.TienGiam,
                    PhiVanChuyen = h.PhiVanChuyen,
                        TrangThai = h.TrangThai,
                        NgayTao = h.NgayTao,
                        TenNguoiNhan = h.TenNguoiNhan,
                        SoDienThoaiNguoiNhan = h.SoDienThoaiNguoiNhan,
                        DiaChiGiaoHang = h.DiaChiGiaoHang,
                        LyDoHuyDon = h.LyDoHuyDon,
                        KhachHang = h.KhachHang != null ? new
                        {
                            IDKhachHang = h.KhachHang.IDKhachHang,
                            TenKhachHang = h.KhachHang.TenKhachHang,
                            SoDienThoai = h.KhachHang.SoDienThoai,
                            Email = h.KhachHang.Email
                        } : null,
                        NhanVien = h.NhanVien != null ? new
                        {
                            IDNhanVien = h.NhanVien.IDNhanVien,
                            TenNhanVien = h.NhanVien.TenNhanVien,
                            SoDienThoai = h.NhanVien.SoDienThoai
                        } : null,
                        PhieuGiamGia = h.PhieuGiamGia != null ? new
                        {
                            IDPhieuGiamGia = h.PhieuGiamGia.IDPhieuGiamGia,
                            MaPhieu = h.PhieuGiamGia.MaCode,
                            TenPhieu = h.PhieuGiamGia.TenPhieu
                        } : null,
                        PhuongThucThanhToan = h.PhuongThucThanhToan != null ? new
                        {
                            IDPhuongThucThanhToan = h.PhuongThucThanhToan.IDPhuongThucThanhToan,
                            TenPhuongThuc = h.PhuongThucThanhToan.TenPhuongThuc
                        } : null,
                        ChiTietHoaDons = h.ChiTietHoaDons.Select(ct => new
                        {
                            IDChiTietHoaDon = ct.IDChiTietHoaDon,
                            MaChiTietHoaDon = ct.MaChiTietHoaDon,
                            SoLuong = ct.SoLuong,
                            DonGia = ct.DonGia,
                            ThanhTien = ct.ThanhTien,
                            SanPhamChiTiet = new
                            {
                                IDSanPhamChiTiet = ct.SanPhamChiTiet.IDSanPhamChiTiet,
                                MaSPChiTiet = ct.SanPhamChiTiet.MaSPChiTiet,
                                GiaBan = ct.SanPhamChiTiet.GiaBan,
                                KichCo = ct.SanPhamChiTiet.KichCo != null ? new { TenKichCo = ct.SanPhamChiTiet.KichCo.TenKichCo } : null,
                                MauSac = ct.SanPhamChiTiet.MauSac != null ? new { TenMauSac = ct.SanPhamChiTiet.MauSac.TenMauSac } : null,
                                HoaTiet = ct.SanPhamChiTiet.HoaTiet != null ? new { TenHoaTiet = ct.SanPhamChiTiet.HoaTiet.TenHoaTiet } : null,
                                SanPham = ct.SanPhamChiTiet.SanPham != null ? new
                                {
                                    IDSanPham = ct.SanPhamChiTiet.SanPham.IDSanPham,
                                    TenSanPham = ct.SanPhamChiTiet.SanPham.TenSanPham,
                                    MaSanPham = ct.SanPhamChiTiet.SanPham.MaSanPham
                                } : null
                            }
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (hoaDon == null)
                {
                    return NotFound();
                }

                return Ok(hoaDon);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy chi tiết hóa đơn: {Message}", ex.Message);
                return StatusCode(500, "Lỗi khi tải chi tiết đơn hàng");
            }
        }

        // POST: api/HoaDons
        [HttpPost]
        public async Task<ActionResult<HoaDon>> CreateHoaDon([FromBody] CreateHoaDonDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Kiểm tra dữ liệu đầu vào
                if (dto.ChiTietHoaDons == null || !dto.ChiTietHoaDons.Any())
                {
                    return BadRequest("Không có sản phẩm nào trong đơn hàng");
                }

                if (dto.TongTien <= 0)
                {
                    return BadRequest("Tổng tiền phải lớn hơn 0");
                }

                // Tạo hóa đơn mới
                var hoaDon = new HoaDon
                {
                    IDHoaDon = Guid.NewGuid(),
                    MaHoaDon = $"HD_{DateTime.Now:yyyyMMddHHmmss}",
                    IDKhachHang = dto.KhachHangId,
                    IDNhanVien = dto.NhanVienId,
                    IDPhieuGiamGia = dto.PhieuGiamGiaId,
                    IDPhuongThucThanhToan = dto.PhuongThucThanhToanId,
                    TongTien = dto.TongTien,
                    TienGiam = dto.TienGiam ?? 0,
                    PhiVanChuyen = dto.PhiVanChuyen ?? 0, // Thêm phí vận chuyển
                    TrangThai = "Chờ xác nhận",
                    TenNguoiNhan = dto.TenNguoiNhan ?? "",
                    SoDienThoaiNguoiNhan = dto.SoDienThoaiNguoiNhan ?? "",
                    DiaChiGiaoHang = dto.DiaChiGiaoHang ?? "",
                    NgayTao = DateTime.UtcNow,
                    NguoiTao = "Customer",
                    TrangThaiHoaDon = true
                };

                _context.HoaDons.Add(hoaDon);

                // Tạo chi tiết hóa đơn từ dữ liệu được gửi lên
                foreach (var chiTiet in dto.ChiTietHoaDons)
                {
                    var chiTietHoaDon = new ChiTietHoaDon
                    {
                        IDChiTietHoaDon = Guid.NewGuid(),
                        MaChiTietHoaDon = $"CTHD_{DateTime.Now:yyyyMMddHHmmss}_{chiTiet.IDSanPhamChiTiet.ToString().Substring(0, 8)}",
                        IDHoaDon = hoaDon.IDHoaDon,
                        IDSanPhamChiTiet = chiTiet.IDSanPhamChiTiet,
                        SoLuong = chiTiet.SoLuong,
                        DonGia = chiTiet.DonGia,
                        ThanhTien = chiTiet.ThanhTien,
                        NgayTao = DateTime.UtcNow,
                        NguoiTao = "Customer",
                        TrangThai = true
                    };

                    _context.ChiTietHoaDons.Add(chiTietHoaDon);

                    // Cập nhật số lượng sản phẩm
                    var sanPhamChiTiet = await _context.SanPhamChiTiets.FindAsync(chiTiet.IDSanPhamChiTiet);
                    if (sanPhamChiTiet != null)
                    {
                        sanPhamChiTiet.SoLuong -= chiTiet.SoLuong;
                        if (sanPhamChiTiet.SoLuong < 0)
                        {
                            return BadRequest($"Sản phẩm {sanPhamChiTiet.MaSPChiTiet} không đủ số lượng");
                        }
                    }
                }

                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetHoaDon), new { id = hoaDon.IDHoaDon }, hoaDon);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo hóa đơn: {Message}", ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError(ex.InnerException, "Inner exception: {Message}", ex.InnerException.Message);
                }
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        // PUT: api/HoaDons/{id}/trangthai
        [HttpPut("{id}/trangthai")]
        public async Task<IActionResult> UpdateTrangThai(Guid id, [FromBody] UpdateTrangThaiDto dto)
        {
            try
            {
                var hoaDon = await _context.HoaDons
                    .Include(h => h.ChiTietHoaDons)
                    .ThenInclude(ct => ct.SanPhamChiTiet)
                    .Include(h => h.KhachHang)
                    .FirstOrDefaultAsync(h => h.IDHoaDon == id);
                
                if (hoaDon == null)
                {
                    return NotFound("Không tìm thấy hóa đơn");
                }

                var oldStatus = hoaDon.TrangThai;

                // Nếu đơn hàng đang chuyển sang trạng thái "Đã hủy", hoàn trả số lượng sản phẩm
                if (dto.TrangThai == "Đã hủy" && hoaDon.TrangThai != "Đã hủy")
                {
                    _logger.LogInformation($"Bắt đầu hoàn trả số lượng sản phẩm cho đơn hàng {id}");
                    
                    foreach (var chiTiet in hoaDon.ChiTietHoaDons)
                    {
                        if (chiTiet.SanPhamChiTiet != null)
                        {
                            var soLuongCu = chiTiet.SanPhamChiTiet.SoLuong;
                            // Hoàn trả số lượng sản phẩm về kho
                            chiTiet.SanPhamChiTiet.SoLuong += chiTiet.SoLuong;
                            
                            _logger.LogInformation($"Hoàn trả {chiTiet.SoLuong} sản phẩm {chiTiet.SanPhamChiTiet.MaSPChiTiet} về kho: {soLuongCu} -> {chiTiet.SanPhamChiTiet.SoLuong}");
                        }
                    }
                    
                    _logger.LogInformation($"Hoàn thành hoàn trả số lượng sản phẩm cho đơn hàng {id}");
                }

                // Cập nhật trạng thái
                hoaDon.TrangThai = dto.TrangThai;
                hoaDon.NguoiCapNhat = dto.NguoiCapNhat;
                hoaDon.LanCapNhatCuoi = dto.LanCapNhatCuoi;
                
                // Lưu lý do hủy đơn nếu có
                if (dto.TrangThai == "Đã hủy" && !string.IsNullOrEmpty(dto.LyDoHuyDon))
                {
                    hoaDon.LyDoHuyDon = dto.LyDoHuyDon;
                }

                // Lưu lịch sử thay đổi trạng thái
                await _orderHistoryService.SaveOrderHistoryAsync(id, oldStatus, dto.TrangThai, dto.NguoiCapNhat, dto.LyDoHuyDon);

                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Updated order {id} status to {dto.TrangThai}");
                
                // Gửi email thông báo thay đổi trạng thái
                if (dto.TrangThai == "Đã hủy")
                {
                    await _emailService.SendOrderCancellationEmailAsync(hoaDon, dto.LyDoHuyDon);
                }
                else
                {
                    await _emailService.SendOrderStatusChangeEmailAsync(hoaDon, oldStatus, dto.TrangThai);
                }

                return Ok(new { message = "Cập nhật trạng thái thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status: {Message}", ex.Message);
                return StatusCode(500, "Lỗi khi cập nhật trạng thái đơn hàng");
            }
        }

        // GET: api/HoaDons/{id}/rollback-options
        [HttpGet("{id}/rollback-options")]
        public async Task<IActionResult> GetRollbackOptions(Guid id)
        {
            try
            {
                var hoaDon = await _context.HoaDons.FindAsync(id);
                if (hoaDon == null)
                {
                    return NotFound("Không tìm thấy hóa đơn");
                }

                var validStatuses = await _orderHistoryService.GetValidRollbackStatusesAsync(id, hoaDon.TrangThai);
                
                return Ok(new 
                { 
                    currentStatus = hoaDon.TrangThai,
                    validRollbackStatuses = validStatuses,
                    canRollback = validStatuses.Any()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rollback options: {Message}", ex.Message);
                return StatusCode(500, "Lỗi khi lấy tùy chọn rollback");
            }
        }

        // POST: api/HoaDons/{id}/rollback
        [HttpPost("{id}/rollback")]
        public async Task<IActionResult> RollbackStatus(Guid id, [FromBody] RollbackStatusDto dto)
        {
            try
            {
                var hoaDon = await _context.HoaDons
                    .Include(h => h.KhachHang)
                    .FirstOrDefaultAsync(h => h.IDHoaDon == id);
                    
                if (hoaDon == null)
                {
                    return NotFound("Không tìm thấy hóa đơn");
                }

                var oldStatus = hoaDon.TrangThai;
                var success = await _orderHistoryService.RollbackOrderStatusAsync(id, dto.TargetStatus, dto.Reason, dto.UpdatedBy);

                if (!success)
                {
                    return BadRequest("Không thể rollback đơn hàng về trạng thái này");
                }

                // Gửi email thông báo rollback
                await _emailService.SendOrderStatusChangeEmailAsync(hoaDon, oldStatus, dto.TargetStatus);

                return Ok(new { message = "Rollback trạng thái thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rolling back order status: {Message}", ex.Message);
                return StatusCode(500, "Lỗi khi rollback trạng thái đơn hàng");
            }
        }

        // GET: api/HoaDons/{id}/history
        [HttpGet("{id}/history")]
        public async Task<IActionResult> GetOrderHistory(Guid id)
        {
            try
            {
                var history = await _orderHistoryService.GetOrderHistoryAsync(id);
                
                var result = history.Select(h => new
                {
                    id = h.IDLichSuHoaDon,
                    trangThai = h.TrangThai,
                    ghiChu = h.GhiChu,
                    ngayTao = h.NgayTao,
                    nguoiTao = h.NguoiTao
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order history: {Message}", ex.Message);
                return StatusCode(500, "Lỗi khi lấy lịch sử đơn hàng");
            }
        }

        // PUT: api/HoaDons/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateHoaDon(Guid id, HoaDon hoaDon)
        {
            if (id != hoaDon.IDHoaDon)
            {
                return BadRequest();
            }

            _context.Entry(hoaDon).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!HoaDonExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/HoaDons/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHoaDon(Guid id)
        {
            try
            {
                var hoaDon = await _context.HoaDons
                    .Include(h => h.ChiTietHoaDons)
                    .ThenInclude(ct => ct.SanPhamChiTiet)
                    .FirstOrDefaultAsync(h => h.IDHoaDon == id);
                
                if (hoaDon == null)
                {
                    return NotFound();
                }

                // Hoàn trả số lượng sản phẩm về kho trước khi xóa đơn hàng
                if (hoaDon.TrangThai != "Đã hủy") // Chỉ hoàn trả nếu đơn hàng chưa bị hủy
                {
                    _logger.LogInformation($"Bắt đầu hoàn trả số lượng sản phẩm cho đơn hàng {id} trước khi xóa");
                    
                    foreach (var chiTiet in hoaDon.ChiTietHoaDons)
                    {
                        if (chiTiet.SanPhamChiTiet != null)
                        {
                            var soLuongCu = chiTiet.SanPhamChiTiet.SoLuong;
                            // Hoàn trả số lượng sản phẩm về kho
                            chiTiet.SanPhamChiTiet.SoLuong += chiTiet.SoLuong;
                            
                            _logger.LogInformation($"Hoàn trả {chiTiet.SoLuong} sản phẩm {chiTiet.SanPhamChiTiet.MaSPChiTiet} về kho: {soLuongCu} -> {chiTiet.SanPhamChiTiet.SoLuong}");
                        }
                    }
                    
                    _logger.LogInformation($"Hoàn thành hoàn trả số lượng sản phẩm cho đơn hàng {id}");
                }

                _context.HoaDons.Remove(hoaDon);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Đã xóa đơn hàng {id} thành công");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting order {id}: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        private bool HoaDonExists(Guid id)
        {
            return _context.HoaDons.Any(e => e.IDHoaDon == id);
        }

        // GET: api/HoaDons/don-hang-can-xac-nhan
        [HttpGet("don-hang-can-xac-nhan")]
        public async Task<ActionResult<object>> GetDonHangCanXacNhan()
        {
            try
            {
                var donHangCanXacNhan = await _context.HoaDons
                    .Where(h => h.TrangThai == "Chờ xác nhận" && !string.IsNullOrEmpty(h.DiaChiGiaoHang))
                    .Include(h => h.KhachHang)
                    .OrderByDescending(h => h.NgayTao)
                    .Take(5)
                    .Select(h => new
                    {
                        id = h.IDHoaDon,
                        maHoaDon = h.MaHoaDon,
                        tenKhachHang = h.KhachHang != null ? h.KhachHang.TenKhachHang : h.TenNguoiNhan,
                        ngayTao = h.NgayTao,
                        tongTien = h.TongTien
                    })
                    .ToListAsync();

                var tongSo = await _context.HoaDons
                    .CountAsync(h => h.TrangThai == "Chờ xác nhận" && !string.IsNullOrEmpty(h.DiaChiGiaoHang));

                return Ok(new
                {
                    tongSo = tongSo,
                    danhSach = donHangCanXacNhan
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy đơn hàng cần xác nhận: {Message}", ex.Message);
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        // GET: api/HoaDons/guest 
        [HttpGet("guest")]
        public async Task<ActionResult<IEnumerable<HoaDon>>> GetHoaDonsByGuest(
            string? search = null, int page = 1, int pageSize = 10)
        {
            try
            {
                if (string.IsNullOrEmpty(search))
                {
                    return BadRequest("Mã đơn hàng là bắt buộc để tìm đơn hàng");
                }

                var query = _context.HoaDons
                    .Include(h => h.KhachHang)
                    .Include(h => h.NhanVien)
                    .Include(h => h.PhieuGiamGia)
                    .Include(h => h.PhuongThucThanhToan)
                    .Include(h => h.ChiTietHoaDons)
                        .ThenInclude(ct => ct.SanPhamChiTiet)
                            .ThenInclude(spct => spct.SanPham)
                    .Where(h => h.TrangThaiHoaDon);

                query = query.Where(h => h.MaHoaDon.Contains(search));

                query = query.OrderByDescending(h => h.NgayTao);

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                var hoaDons = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                Response.Headers.Append("X-Total-Count", totalCount.ToString());
                Response.Headers.Append("X-Total-Pages", totalPages.ToString());
                Response.Headers.Append("X-Current-Page", page.ToString());
                Response.Headers.Append("X-Page-Size", pageSize.ToString());

                return Ok(hoaDons);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tìm đơn hàng cho khách vãng lai với mã đơn hàng {Search}", search);
                return StatusCode(500, "Lỗi nội bộ server");
            }
        }

        // GET: api/HoaDons/customer/{customerId} - Cho người dùng đã đăng nhập
        [HttpGet("customer/{customerId}")]
        public async Task<ActionResult<IEnumerable<HoaDon>>> GetHoaDonsByCustomer(
            Guid customerId, string? search = null, string? fromDate = null, string? toDate = null, 
            int page = 1, int pageSize = 10)
        {
            try
            {
                var query = _context.HoaDons
                    .Include(h => h.KhachHang)
                    .Include(h => h.NhanVien)
                    .Include(h => h.PhieuGiamGia)
                    .Include(h => h.PhuongThucThanhToan)
                    .Include(h => h.ChiTietHoaDons)
                        .ThenInclude(ct => ct.SanPhamChiTiet)
                            .ThenInclude(spct => spct.SanPham)
                    .Where(h => h.IDKhachHang == customerId && h.TrangThaiHoaDon)
                    .AsQueryable();

                // Tìm kiếm theo mã đơn hàng
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(h => h.MaHoaDon.Contains(search));
                }

                // Lọc theo ngày từ
                if (!string.IsNullOrEmpty(fromDate) && DateTime.TryParse(fromDate, out var fromDateParsed))
                {
                    query = query.Where(h => h.NgayTao.Date >= fromDateParsed.Date);
                }

                // Lọc theo ngày đến
                if (!string.IsNullOrEmpty(toDate) && DateTime.TryParse(toDate, out var toDateParsed))
                {
                    query = query.Where(h => h.NgayTao.Date <= toDateParsed.Date);
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var hoaDons = await query
                    .OrderByDescending(h => h.NgayTao)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                Response.Headers.Append("X-Total-Count", totalCount.ToString());
                Response.Headers.Append("X-Total-Pages", totalPages.ToString());
                Response.Headers.Append("X-Current-Page", page.ToString());
                Response.Headers.Append("X-Page-Size", pageSize.ToString());

                return Ok(hoaDons);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy đơn hàng cho khách hàng {CustomerId}", customerId);
                return StatusCode(500, "Lỗi nội bộ server");
            }
        }
    }

    public class CreateHoaDonDto
    {
        public Guid? KhachHangId { get; set; }
        public Guid? NhanVienId { get; set; }
        public Guid? PhieuGiamGiaId { get; set; }
        public Guid PhuongThucThanhToanId { get; set; }
        public decimal TongTien { get; set; }
        public decimal? TienGiam { get; set; }
        public decimal? PhiVanChuyen { get; set; }
        public string TenNguoiNhan { get; set; }
        public string SoDienThoaiNguoiNhan { get; set; }
        public string DiaChiGiaoHang { get; set; }
        public string GhiChu { get; set; }
        public List<ChiTietHoaDonDto> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDonDto>();
    }

    public class ChiTietHoaDonDto
    {
        public Guid IDSanPhamChiTiet { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien { get; set; }
    }

    public class UpdateTrangThaiDto
    {
        public string TrangThai { get; set; }
        public string NguoiCapNhat { get; set; }
        public DateTime LanCapNhatCuoi { get; set; }
        public string? LyDoHuyDon { get; set; }
    }

    public class RollbackStatusDto
    {
        public string TargetStatus { get; set; }
        public string Reason { get; set; }
        public string UpdatedBy { get; set; }
    }
}