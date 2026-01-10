using AutoMapper;
using BanQuanAu1.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;
using QuanApi.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using QuanApi.Services;
using System.Text;

namespace QuanApi.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class KhachHangController : ControllerBase
    {
        private readonly BanQuanAu1DbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<KhachHangController> _logger;
        private readonly IEmailService _emailService;

        public KhachHangController(BanQuanAu1DbContext context, IMapper mapper, ILogger<KhachHangController> logger, IEmailService emailService)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _emailService = emailService;
        }

        [HttpGet]
        [HttpGet("Index")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<KhachHangDto>>> GetKhachHang(
            string? search = null,
            int pageNumber = 1,
            int pageSize = 10,
            string? sortBy = "NgayTao",
            bool sortAscending = false)
        {
            _logger.LogInformation("Đang lấy danh sách khách hàng với tìm kiếm: {Search}, trang: {PageNumber}, kích thước trang: {PageSize}", search, pageNumber, pageSize);

            var query = _context.KhachHang.Include(kh => kh.DiaChis).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(kh =>
                    kh.MaKhachHang.Contains(search) ||
                    kh.TenKhachHang.Contains(search) ||
                    (kh.Email != null && kh.Email.Contains(search)) ||
                    kh.SoDienThoai.Contains(search));
            }

            switch (sortBy?.ToLower())
            {
                case "makhachhang":
                    query = sortAscending ? query.OrderBy(kh => kh.MaKhachHang) : query.OrderByDescending(kh => kh.MaKhachHang);
                    break;
                case "tenkhachhang":
                    query = sortAscending ? query.OrderBy(kh => kh.TenKhachHang) : query.OrderByDescending(kh => kh.TenKhachHang);
                    break;
                case "ngaytao":
                    query = sortAscending ? query.OrderBy(kh => kh.NgayTao) : query.OrderByDescending(kh => kh.NgayTao);
                    break;
                default:
                    query = query.OrderByDescending(kh => kh.NgayTao);
                    break;
            }

            var totalCount = await query.CountAsync();
            var khachHangs = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            Response.Headers.Append("X-Total-Count", totalCount.ToString());
            Response.Headers.Append("X-Page-Size", pageSize.ToString());
            Response.Headers.Append("X-Current-Page", pageNumber.ToString());
            Response.Headers.Append("X-Total-Pages", ((int)Math.Ceiling((double)totalCount / pageSize)).ToString());

            return Ok(_mapper.Map<IEnumerable<KhachHangDto>>(khachHangs));
        }

        [HttpGet("{id}")]
        [HttpGet("Details/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<KhachHangDto>> GetKhachHang(Guid id)
        {
            _logger.LogInformation("Đang lấy chi tiết khách hàng với ID: {CustomerId}", id);

            var khachHang = await _context.KhachHang
                                          .Include(kh => kh.DiaChis)
                                          .FirstOrDefaultAsync(kh => kh.IDKhachHang == id);

            if (khachHang == null)
            {
                _logger.LogWarning("Không tìm thấy khách hàng với ID: {CustomerId}", id);
                return NotFound();
            }

            return Ok(_mapper.Map<KhachHangDto>(khachHang));
        }

        [HttpPost]
        [HttpPost("Create")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<KhachHangDto>> PostKhachHang([FromBody] CreateKhachHangDto createKhachHangDto)
        {
            _logger.LogInformation("Đang tạo khách hàng mới với Mã khách hàng: {MaKhachHang}", createKhachHangDto.MaKhachHang);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Dữ liệu đầu vào không hợp lệ khi tạo khách hàng.");
                return BadRequest(ModelState);
            }

            if (await _context.KhachHang.AnyAsync(kh => kh.MaKhachHang == createKhachHangDto.MaKhachHang))
            {
                ModelState.AddModelError("MaKhachHang", "Mã khách hàng đã tồn tại.");
                _logger.LogWarning("Tạo khách hàng thất bại: Mã khách hàng '{MaKhachHang}' đã tồn tại.", createKhachHangDto.MaKhachHang);
                return Conflict(new ValidationProblemDetails(ModelState));
            }
            if (!string.IsNullOrEmpty(createKhachHangDto.Email) && await _context.KhachHang.AnyAsync(kh => kh.Email == createKhachHangDto.Email))
            {
                ModelState.AddModelError("Email", "Email đã tồn tại.");
                _logger.LogWarning("Tạo khách hàng thất bại: Email '{Email}' đã tồn tại.", createKhachHangDto.Email);
                return Conflict(new ValidationProblemDetails(ModelState));
            }
            if (!string.IsNullOrEmpty(createKhachHangDto.SoDienThoai) && await _context.KhachHang.AnyAsync(kh => kh.SoDienThoai == createKhachHangDto.SoDienThoai))
            {
                ModelState.AddModelError("SoDienThoai", "Số điện thoại đã tồn tại.");
                _logger.LogWarning("Tạo khách hàng thất bại: Số điện thoại '{SoDienThoai}' đã tồn tại.", createKhachHangDto.SoDienThoai);
                return Conflict(new ValidationProblemDetails(ModelState));
            }

            var khachHang = _mapper.Map<KhachHang>(createKhachHangDto);

            khachHang.IDKhachHang = Guid.NewGuid();
            khachHang.NgayTao = DateTime.UtcNow;
            khachHang.NguoiTao = User.Identity?.Name ?? "System";
            khachHang.LanCapNhatCuoi = null;
            khachHang.NguoiCapNhat = null;
            khachHang.TrangThai = true;

            if (string.IsNullOrEmpty(createKhachHangDto.MatKhau))
            {
                ModelState.AddModelError("MatKhau", "Mật khẩu là bắt buộc khi tạo mới khách hàng.");
                _logger.LogWarning("Tạo khách hàng thất bại: Mật khẩu không được cung cấp.");
                return BadRequest(new ValidationProblemDetails(ModelState));
            }
            khachHang.MatKhau = createKhachHangDto.MatKhau;

            if (createKhachHangDto.DiaChis != null && createKhachHangDto.DiaChis.Any())
            {
                khachHang.DiaChis = new List<DiaChi>();
                var duplicateMaDiaChiInDto = createKhachHangDto.DiaChis.GroupBy(d => d.MaDiaChi)
                                                                 .Where(g => g.Count() > 1)
                                                                 .Select(g => g.Key)
                                                                 .ToList();
                if (duplicateMaDiaChiInDto.Any())
                {
                    foreach (var ma in duplicateMaDiaChiInDto)
                    {
                        ModelState.AddModelError("DiaChis", $"Mã địa chỉ '{ma}' bị trùng trong danh sách địa chỉ gửi lên.");
                    }
                    _logger.LogWarning("Tạo khách hàng thất bại: Mã địa chỉ trùng lặp trong DTO đầu vào.");
                    return BadRequest(new ValidationProblemDetails(ModelState));
                }

                foreach (var diaChiDto in createKhachHangDto.DiaChis)
                {
                    if (await _context.DiaChis.AnyAsync(d => d.IDKhachHang == khachHang.IDKhachHang && d.MaDiaChi == diaChiDto.MaDiaChi))
                    {
                        ModelState.AddModelError("DiaChis", $"Mã địa chỉ '{diaChiDto.MaDiaChi}' đã tồn tại cho khách hàng này.");
                        _logger.LogWarning("Tạo khách hàng thất bại: Mã địa chỉ '{MaDiaChi}' đã tồn tại cho khách hàng này.", diaChiDto.MaDiaChi);
                        return Conflict(new ValidationProblemDetails(ModelState));
                    }

                    var diaChi = _mapper.Map<DiaChi>(diaChiDto);
                    diaChi.IDDiaChi = Guid.NewGuid();
                    diaChi.IDKhachHang = khachHang.IDKhachHang;
                    diaChi.NgayTao = DateTime.UtcNow;
                    diaChi.NguoiTao = User.Identity?.Name ?? "System";
                    diaChi.LanCapNhatCuoi = null;
                    diaChi.NguoiCapNhat = null;
                    diaChi.TrangThai = true;

                    khachHang.DiaChis.Add(diaChi);
                }
            }

            _context.KhachHang.Add(khachHang);
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Đã tạo khách hàng '{MaKhachHang}' thành công với ID: {CustomerId}", khachHang.MaKhachHang, khachHang.IDKhachHang);

                if (!string.IsNullOrEmpty(khachHang.Email))
                {
                    var subject = "Chào mừng bạn đến với Cửa hàng bán quần âu Dazio!";
                    var emailBody = new StringBuilder();
                    emailBody.AppendLine($"<p>Xin chào <strong>{khachHang.TenKhachHang}</strong>,</p>");
                    emailBody.AppendLine("<p>Bạn đã đăng ký tài khoản thành công tại Hệ thống Quản lý Bán Quần Áo của chúng tôi.</p>");
                    emailBody.AppendLine("<p>Dưới đây là thông tin chi tiết tài khoản của bạn:</p>");
                    emailBody.AppendLine("<ul>");
                    emailBody.AppendLine($"<li><strong>Mã khách hàng:</strong> {khachHang.MaKhachHang}</li>");
                    emailBody.AppendLine($"<li><strong>Tên khách hàng:</strong> {khachHang.TenKhachHang}</li>");
                    emailBody.AppendLine($"<li><strong>Email:</strong> {khachHang.Email}</li>");
                    emailBody.AppendLine($"<li><strong>Số điện thoại:</strong> {khachHang.SoDienThoai}</li>");
                    emailBody.AppendLine($"<li><strong>Trạng thái:</strong> {(khachHang.TrangThai ? "Kích hoạt" : "Ẩn")}</li>");
                    emailBody.AppendLine("</ul>");

                    if (khachHang.DiaChis != null && khachHang.DiaChis.Any())
                    {
                        emailBody.AppendLine("<p>Các địa chỉ đã đăng ký của bạn:</p>");
                        emailBody.AppendLine("<ol>");
                        foreach (var diaChi in khachHang.DiaChis)
                        {
                            emailBody.AppendLine($"<li><strong>Mã địa chỉ:</strong> {diaChi.MaDiaChi}, <strong>Chi tiết:</strong> {diaChi.DiaChiChiTiet}");
                            if (!string.IsNullOrEmpty(diaChi.TenNguoiNhan))
                            {
                                emailBody.AppendLine($", <strong>Người nhận:</strong> {diaChi.TenNguoiNhan}");
                            }
                            if (!string.IsNullOrEmpty(diaChi.SdtNguoiNhan))
                            {
                                emailBody.AppendLine($", <strong>SĐT người nhận:</strong> {diaChi.SdtNguoiNhan}");
                            }
                            emailBody.AppendLine($"</li>");
                        }
                        emailBody.AppendLine("</ol>");
                    }
                    else
                    {
                        emailBody.AppendLine("<p>Bạn chưa có địa chỉ nào được đăng ký.</p>");
                    }

                    emailBody.AppendLine("<p>Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!</p>");
                    emailBody.AppendLine("<p>Trân trọng,</p>");
                    emailBody.AppendLine("<p><strong>Cửa hàng bán quần âu Dazio</strong></p>");

                    try
                    {
                        await _emailService.SendEmailAsync(khachHang.Email, subject, emailBody.ToString());
                        _logger.LogInformation("Đã gửi email thông báo thành công tới khách hàng: {Email}", khachHang.Email);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi khi gửi email thông báo cho khách hàng {Email}: {Message}", khachHang.Email, ex.Message);
                    }
                }
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Lỗi cơ sở dữ liệu khi tạo khách hàng '{MaKhachHang}'.", khachHang.MaKhachHang);
                return StatusCode(500, $"Lỗi cơ sở dữ liệu khi tạo: {ex.InnerException?.Message ?? ex.Message}");
            }

            var khachHangDto = _mapper.Map<KhachHangDto>(khachHang);

            return CreatedAtAction(nameof(GetKhachHang), new { id = khachHang.IDKhachHang }, khachHangDto);
        }

        [HttpPut("{id}")]
        [HttpPost("Edit/{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> PutKhachHang(Guid id, [FromBody] UpdateKhachHangDto updateKhachHangDto)
        {
            _logger.LogInformation("Đang cập nhật khách hàng với ID: {CustomerId}", id);

            if (id != updateKhachHangDto.IDKhachHang)
            {
                _logger.LogWarning("ID trong URL ({UrlId}) không khớp với ID khách hàng trong dữ liệu ({DtoId}).", id, updateKhachHangDto.IDKhachHang);
                return BadRequest("ID trong URL không khớp với ID khách hàng trong dữ liệu.");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Dữ liệu đầu vào không hợp lệ khi cập nhật khách hàng ID: {CustomerId}", id);
                return BadRequest(ModelState);
            }

            var originalKhachHang = await _context.KhachHang
                                                    .Include(kh => kh.DiaChis)
                                                    .FirstOrDefaultAsync(kh => kh.IDKhachHang == id);

            if (originalKhachHang == null)
            {
                _logger.LogWarning("Không tìm thấy khách hàng với ID: {CustomerId} để cập nhật.", id);
                return NotFound("Không tìm thấy khách hàng để cập nhật.");
            }

            if (await _context.KhachHang.AnyAsync(kh => kh.MaKhachHang == updateKhachHangDto.MaKhachHang && kh.IDKhachHang != id))
            {
                ModelState.AddModelError("MaKhachHang", "Mã khách hàng đã tồn tại.");
                _logger.LogWarning("Cập nhật khách hàng ID: {CustomerId} thất bại: Mã khách hàng '{MaKhachHang}' đã tồn tại.", id, updateKhachHangDto.MaKhachHang);
                return Conflict(new ValidationProblemDetails(ModelState));
            }
            if (!string.IsNullOrEmpty(updateKhachHangDto.Email) && await _context.KhachHang.AnyAsync(kh => kh.Email == updateKhachHangDto.Email && kh.IDKhachHang != id))
            {
                ModelState.AddModelError("Email", "Email đã tồn tại.");
                _logger.LogWarning("Cập nhật khách hàng ID: {CustomerId} thất bại: Email '{Email}' đã tồn tại.", id, updateKhachHangDto.Email);
                return Conflict(new ValidationProblemDetails(ModelState));
            }
            if (!string.IsNullOrEmpty(updateKhachHangDto.SoDienThoai) && await _context.KhachHang.AnyAsync(kh => kh.SoDienThoai == updateKhachHangDto.SoDienThoai && kh.IDKhachHang != id))
            {
                ModelState.AddModelError("SoDienThoai", "Số điện thoại đã tồn tại.");
                _logger.LogWarning("Cập nhật khách hàng ID: {CustomerId} thất bại: Số điện thoại '{SoDienThoai}' đã tồn tại.", id, updateKhachHangDto.SoDienThoai);
                return Conflict(new ValidationProblemDetails(ModelState));
            }

            _mapper.Map(updateKhachHangDto, originalKhachHang);

            originalKhachHang.LanCapNhatCuoi = DateTime.UtcNow;
            originalKhachHang.NguoiCapNhat = User.Identity?.Name ?? "System";

            if (!string.IsNullOrEmpty(updateKhachHangDto.MatKhau))
            {
                originalKhachHang.MatKhau = updateKhachHangDto.MatKhau;
            }

            var existingDiaChis = originalKhachHang.DiaChis.ToList();
            var updatedDiaChisDto = updateKhachHangDto.DiaChis ?? new List<DiaChiDto>();

            var duplicateMaDiaChiInDto = updatedDiaChisDto.GroupBy(d => d.MaDiaChi)
                                                           .Where(g => g.Count() > 1)
                                                           .Select(g => g.Key)
                                                           .ToList();
            if (duplicateMaDiaChiInDto.Any())
            {
                foreach (var ma in duplicateMaDiaChiInDto)
                {
                    ModelState.AddModelError("DiaChis", $"Mã địa chỉ '{ma}' bị trùng trong danh sách địa chỉ gửi lên.");
                }
                _logger.LogWarning("Cập nhật khách hàng ID: {CustomerId} thất bại: Mã địa chỉ trùng lặp trong DTO đầu vào.", id);
                return BadRequest(new ValidationProblemDetails(ModelState));
            }

            foreach (var existingDiaChi in existingDiaChis)
            {
                if (!updatedDiaChisDto.Any(d => d.IDDiaChi == existingDiaChi.IDDiaChi))
                {
                    _context.DiaChis.Remove(existingDiaChi);
                    _logger.LogInformation("Xóa địa chỉ ID: {DiaChiId} của khách hàng ID: {CustomerId}", existingDiaChi.IDDiaChi, id);
                }
            }

            foreach (var diaChiDto in updatedDiaChisDto)
            {
                var existingDiaChi = existingDiaChis.FirstOrDefault(d => d.IDDiaChi == diaChiDto.IDDiaChi);

                if (existingDiaChi == null)
                {
                    if (await _context.DiaChis.AnyAsync(d => d.IDKhachHang == originalKhachHang.IDKhachHang && d.MaDiaChi == diaChiDto.MaDiaChi))
                    {
                        ModelState.AddModelError("DiaChis", $"Mã địa chỉ '{diaChiDto.MaDiaChi}' đã tồn tại cho khách hàng này.");
                        _logger.LogWarning("Cập nhật khách hàng ID: {CustomerId} thất bại: Mã địa chỉ '{MaDiaChi}' đã tồn tại cho khách hàng này.", id, diaChiDto.MaDiaChi);
                        return Conflict(new ValidationProblemDetails(ModelState));
                    }

                    var newDiaChi = _mapper.Map<DiaChi>(diaChiDto);
                    newDiaChi.IDDiaChi = Guid.NewGuid();
                    newDiaChi.IDKhachHang = originalKhachHang.IDKhachHang;
                    newDiaChi.NgayTao = DateTime.UtcNow;
                    newDiaChi.NguoiTao = User.Identity?.Name ?? "System";
                    newDiaChi.LanCapNhatCuoi = null;
                    newDiaChi.NguoiCapNhat = null;
                    newDiaChi.TrangThai = true;

                    _context.DiaChis.Add(newDiaChi);
                    _logger.LogInformation("Thêm địa chỉ mới ID: {DiaChiId} cho khách hàng ID: {CustomerId}", newDiaChi.IDDiaChi, id);
                }
                else
                {
                    if (await _context.DiaChis.AnyAsync(d => d.IDKhachHang == originalKhachHang.IDKhachHang && d.MaDiaChi == diaChiDto.MaDiaChi && d.IDDiaChi != diaChiDto.IDDiaChi))
                    {
                        ModelState.AddModelError("DiaChis", $"Mã địa chỉ '{diaChiDto.MaDiaChi}' đã tồn tại cho một địa chỉ khác của khách hàng này.");
                        _logger.LogWarning("Cập nhật khách hàng ID: {CustomerId} thất bại: Mã địa chỉ '{MaDiaChi}' đã tồn tại cho địa chỉ khác.", id, diaChiDto.MaDiaChi);
                        return Conflict(new ValidationProblemDetails(ModelState));
                    }

                    _mapper.Map(diaChiDto, existingDiaChi);
                    existingDiaChi.LanCapNhatCuoi = DateTime.UtcNow;
                    existingDiaChi.NguoiCapNhat = User.Identity?.Name ?? "System";
                    _logger.LogInformation("Cập nhật địa chỉ ID: {DiaChiId} của khách hàng ID: {CustomerId}", existingDiaChi.IDDiaChi, id);
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Đã cập nhật khách hàng ID: {CustomerId} thành công.", id);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!KhachHangExists(id))
                {
                    _logger.LogWarning(ex, "Cập nhật khách hàng ID: {CustomerId} thất bại: Khách hàng không tồn tại (Concurrency issue).", id);
                    return NotFound();
                }
                else
                {
                    _logger.LogError(ex, "Lỗi đồng thời khi cập nhật khách hàng ID: {CustomerId}.", id);
                    throw;
                }
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Lỗi cơ sở dữ liệu khi cập nhật khách hàng ID: {CustomerId}.", id);
                return StatusCode(500, $"Lỗi cơ sở dữ liệu khi cập nhật: {ex.InnerException?.Message ?? ex.Message}");
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        [HttpDelete("Delete/{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteKhachHang(Guid id)
        {
            _logger.LogInformation("Đang xóa khách hàng với ID: {CustomerId}", id);

            var khachHang = await _context.KhachHang.FindAsync(id);
            if (khachHang == null)
            {
                _logger.LogWarning("Không tìm thấy khách hàng với ID: {CustomerId} để xóa.", id);
                return NotFound();
            }

            _context.KhachHang.Remove(khachHang);
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Đã xóa khách hàng ID: {CustomerId} thành công.", id);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Lỗi cơ sở dữ liệu khi xóa khách hàng ID: {CustomerId}.", id);
                return StatusCode(500, $"Lỗi cơ sở dữ liệu khi xóa: {ex.InnerException?.Message ?? ex.Message}");
            }

            return NoContent();
        }

        private bool KhachHangExists(Guid id)
        {
            return _context.KhachHang.Any(e => e.IDKhachHang == id);
        }

        // API endpoint để lấy địa chỉ của khách hàng
        [HttpGet("{id}/addresses")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<DiaChiDto>>> GetCustomerAddresses(Guid id)
        {
            _logger.LogInformation("Đang lấy danh sách địa chỉ của khách hàng ID: {CustomerId}", id);

            var khachHang = await _context.KhachHang
                .Include(kh => kh.DiaChis)
                .FirstOrDefaultAsync(kh => kh.IDKhachHang == id);

            if (khachHang == null)
            {
                _logger.LogWarning("Không tìm thấy khách hàng với ID: {CustomerId}", id);
                return NotFound();
            }

            var addresses = khachHang.DiaChis?.Where(d => d.TrangThai) ?? new List<DiaChi>();
            var addressDtos = _mapper.Map<IEnumerable<DiaChiDto>>(addresses);

            return Ok(addressDtos);
        }

        // API endpoint để lấy địa chỉ mặc định của khách hàng
        [HttpGet("{id}/default-address")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DiaChiDto>> GetDefaultAddress(Guid id)
        {
            _logger.LogInformation("Đang lấy địa chỉ mặc định của khách hàng ID: {CustomerId}", id);

            var khachHang = await _context.KhachHang
                .Include(kh => kh.DiaChis)
                .FirstOrDefaultAsync(kh => kh.IDKhachHang == id);

            if (khachHang == null)
            {
                _logger.LogWarning("Không tìm thấy khách hàng với ID: {CustomerId}", id);
                return NotFound();
            }

            var defaultAddress = khachHang.DiaChis?.FirstOrDefault(d => d.TrangThai && d.LaMacDinh);
            
            if (defaultAddress == null)
            {
                return NotFound("Không tìm thấy địa chỉ mặc định");
            }

            var addressDto = _mapper.Map<DiaChiDto>(defaultAddress);
            return Ok(addressDto);
        }
    }
}