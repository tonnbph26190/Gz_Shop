using AutoMapper;
using Azure;
using BanQuanAu1.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanApi.Dtos;
using QuanApi.Services;

[ApiController]
[Route("api/[controller]")]
public class KhachHangController : ControllerBase
{
    private readonly BanQuanAu1DbContext _context;
    private readonly IKhachHangService _khachHangService;
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
    public async Task<ActionResult<KhachHangDto>> GetKhachHangById(Guid id)
    {
        var khachHang = await _khachHangService.GetKhachHangByIdAsync(id);

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
    public async Task<ActionResult<KhachHangDto>> PostKhachHang(
    [FromBody] CreateKhachHangDto createKhachHangDto)
    {
        _logger.LogInformation("Đang tạo khách hàng mới với Mã khách hàng: {MaKhachHang}",
            createKhachHangDto.MaKhachHang);

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var khachHang = await _khachHangService.CreateKhachHangAsync(
                createKhachHangDto,
                User.Identity?.Name);

            var khachHangDto = _mapper.Map<KhachHangDto>(khachHang);

            return CreatedAtAction(nameof(GetKhachHangById),
                new { id = khachHang.IDKhachHang },
                khachHangDto);
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message switch
            {
                "DUPLICATE_MAKHACHHANG" => Conflict(new { message = "Mã khách hàng đã tồn tại." }),
                "DUPLICATE_EMAIL" => Conflict(new { message = "Email đã tồn tại." }),
                "DUPLICATE_PHONE" => Conflict(new { message = "Số điện thoại đã tồn tại." }),
                "PASSWORD_REQUIRED" => BadRequest(new { message = "Mật khẩu là bắt buộc." }),
                "DUPLICATE_MADIACHI_IN_DTO" => BadRequest(new { message = "Mã địa chỉ bị trùng trong danh sách." }),
                _ => StatusCode(500, new { message = ex.Message })
            };
        }
    }
    [HttpPut("{id}")]
    [HttpPost("Edit/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PutKhachHang(Guid id, [FromBody] UpdateKhachHangDto dto)
    {
        _logger.LogInformation("Đang cập nhật khách hàng với ID: {CustomerId}", id);

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await _khachHangService.UpdateAsync(id, dto, User.Identity?.Name);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Lỗi DB khi cập nhật khách hàng ID: {CustomerId}", id);
            return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
        }
    }
    [HttpDelete("{id}")]
    [HttpDelete("Delete/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteKhachHang(Guid id)
    {
        _logger.LogInformation("Đang xóa khách hàng với ID: {CustomerId}", id);

        try
        {
            await _khachHangService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Lỗi DB khi xóa khách hàng ID: {CustomerId}", id);
            return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
        }
    }
    // KHÔNG còn dùng _context trong controller
    private async Task<bool> KhachHangExists(Guid id)
    {
        return await _khachHangService.ExistsAsync(id);
    }

    // API endpoint để lấy địa chỉ của khách hàng
    [HttpGet("{id}/addresses")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<DiaChiDto>>> GetCustomerAddresses(Guid id)
    {
        _logger.LogInformation("Đang lấy danh sách địa chỉ của khách hàng ID: {CustomerId}", id);

        try
        {
            var addresses = await _khachHangService.GetAddressesAsync(id);
            var addressDtos = _mapper.Map<IEnumerable<DiaChiDto>>(addresses);
            return Ok(addressDtos);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
    // API endpoint để lấy địa chỉ mặc định của khách hàng
    [HttpGet("{id}/default-address")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DiaChiDto>> GetDefaultAddress(Guid id)
    {
        _logger.LogInformation("Đang lấy địa chỉ mặc định của khách hàng ID: {CustomerId}", id);

        try
        {
            var defaultAddress = await _khachHangService.GetDefaultAddressAsync(id);

            if (defaultAddress == null)
            {
                return NotFound("Không tìm thấy địa chỉ mặc định");
            }

            var addressDto = _mapper.Map<DiaChiDto>(defaultAddress);
            return Ok(addressDto);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }


}
