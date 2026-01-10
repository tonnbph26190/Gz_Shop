using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BanQuanAu1.Web.Data;
using QuanApi.Data;
using Microsoft.Extensions.Logging;
using AutoMapper;
using QuanApi.Dtos;
using System.Text.Json;
using System.Security.Claims;
namespace QuanApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NhanVienController : ControllerBase
    {
        private readonly BanQuanAu1DbContext _context;
        private readonly ILogger<NhanVienController> _logger;
        private readonly IMapper _mapper;

        public NhanVienController(BanQuanAu1DbContext context, ILogger<NhanVienController> logger, IMapper mapper)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
        }

        // GET: api/NhanVien
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResultGeneric<NhanVienResponseDto>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PagedResultGeneric<NhanVienResponseDto>>> GetNhanViens([FromQuery] NhanVienFilterDto filter)
        {
            _logger.LogInformation("Retrieving employees with filter: {@Filter}", filter);
            try
            {
                var query = _context.NhanViens.Include(n => n.VaiTro).AsQueryable();


                if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                {
                    query = query.Where(nv =>
                        nv.MaNhanVien.Contains(filter.SearchTerm) ||
                        nv.TenNhanVien.Contains(filter.SearchTerm) ||
                        nv.Email.Contains(filter.SearchTerm) ||
                        nv.SoDienThoai.Contains(filter.SearchTerm) ||
                        (nv.QueQuan != null && nv.QueQuan.Contains(filter.SearchTerm)) ||
                        (nv.CCCD != null && nv.CCCD.Contains(filter.SearchTerm)));
                }

                if (filter.IDVaiTro.HasValue && filter.IDVaiTro.Value != Guid.Empty)
                {
                    query = query.Where(nv => nv.IDVaiTro == filter.IDVaiTro.Value);
                }

                if (filter.TrangThai.HasValue)
                {
                    query = query.Where(nv => nv.TrangThai == filter.TrangThai.Value);
                }


                if (!string.IsNullOrWhiteSpace(filter.SortBy))
                {
                    switch (filter.SortBy.ToLower())
                    {
                        case "manhanvien":
                            query = filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(nv => nv.MaNhanVien) : query.OrderBy(nv => nv.MaNhanVien);
                            break;
                        case "tennhanvien":
                            query = filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(nv => nv.TenNhanVien) : query.OrderBy(nv => nv.TenNhanVien);
                            break;
                        case "email":
                            query = filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(nv => nv.Email) : query.OrderBy(nv => nv.Email);
                            break;
                        case "sodienthoai":
                            query = filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(nv => nv.SoDienThoai) : query.OrderBy(nv => nv.SoDienThoai);
                            break;
                        case "ngaysinh":
                            query = filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(nv => nv.NgaySinh) : query.OrderBy(nv => nv.NgaySinh);
                            break;
                        case "gioitinh":
                            query = filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(nv => nv.GioiTinh) : query.OrderBy(nv => nv.GioiTinh);
                            break;
                        case "quequan":
                            query = filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(nv => nv.QueQuan) : query.OrderBy(nv => nv.QueQuan);
                            break;
                        case "cccd":
                            query = filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(nv => nv.CCCD) : query.OrderBy(nv => nv.CCCD);
                            break;
                        case "tenvaitro":
                            query = filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(nv => nv.VaiTro.TenVaiTro) : query.OrderBy(nv => nv.VaiTro.TenVaiTro);
                            break;
                        case "trangthai":
                            query = filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(nv => nv.TrangThai) : query.OrderBy(nv => nv.TrangThai);
                            break;
                        case "ngaytao":
                            query = filter.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(nv => nv.NgayTao) : query.OrderBy(nv => nv.NgayTao);
                            break;
                        default:
                            query = query.OrderBy(nv => nv.NgayTao);
                            break;
                    }
                }
                else
                {
                    query = query.OrderBy(nv => nv.NgayTao);
                }


                var totalCount = await query.CountAsync();
                var items = await query
                    .Skip((filter.PageNumber - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToListAsync();

                var itemDtos = _mapper.Map<List<NhanVienResponseDto>>(items);

                var pagedResult = new PagedResultGeneric<NhanVienResponseDto>
                {
                    Data = itemDtos,
                    TotalCount = totalCount,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize,
                };

                return Ok(pagedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employees with filter: {@Filter}", filter);
                return StatusCode(500, "Internal server error when retrieving employees.");
            }
        }


        // GET: api/NhanVien/5
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(NhanVienResponseDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<NhanVienResponseDto>> GetNhanVien(Guid id)
        {
            _logger.LogInformation("Retrieving employee with ID: {Id}", id);
            try
            {
                var nhanVien = await _context.NhanViens
                    .Include(n => n.VaiTro)
                    .FirstOrDefaultAsync(m => m.IDNhanVien == id);

                if (nhanVien == null)
                {
                    _logger.LogWarning("Employee with ID: {Id} not found.", id);
                    return NotFound($"Employee with ID {id} not found.");
                }

                var nhanVienDto = _mapper.Map<NhanVienResponseDto>(nhanVien);
                return Ok(nhanVienDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employee with ID: {Id}", id);
                return StatusCode(500, $"Internal server error when retrieving employee with ID {id}.");
            }
        }

        // POST: api/NhanVien

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(NhanVienResponseDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<NhanVienResponseDto>> PostNhanVien([FromBody] NhanVienCreateDto nhanVienCreateDto)
        {
            _logger.LogInformation("Attempting to create new employee: {@Dto}", nhanVienCreateDto);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for employee creation. Errors: {@ModelStateErrors}",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList());
                return BadRequest(ModelState);
            }

            try
            {

                var emailExists = await _context.NhanViens.AnyAsync(nv => nv.Email == nhanVienCreateDto.Email);
                if (emailExists)
                {
                    _logger.LogWarning("Email '{Email}' already exists.", nhanVienCreateDto.Email);
                    return BadRequest($"Email '{nhanVienCreateDto.Email}' đã tồn tại.");
                }

                var maNhanVienExists = await _context.NhanViens.AnyAsync(nv => nv.MaNhanVien == nhanVienCreateDto.MaNhanVien);
                if (maNhanVienExists)
                {
                    _logger.LogWarning("MaNhanVien '{MaNhanVien}' already exists.", nhanVienCreateDto.MaNhanVien);
                    return BadRequest($"Mã nhân viên '{nhanVienCreateDto.MaNhanVien}' đã tồn tại.");
                }

                if (nhanVienCreateDto.IDVaiTro == Guid.Empty || !await _context.VaiTro.AnyAsync(v => v.IDVaiTro == nhanVienCreateDto.IDVaiTro))
                {
                    var adminRole = await _context.VaiTro.FirstOrDefaultAsync(v => v.TenVaiTro.ToLower() == "admin");
                    if (adminRole == null)
                    {
                        return BadRequest("Không tìm thấy vai trò Admin trong hệ thống. Vui lòng tạo trước.");
                    }
                    nhanVienCreateDto.IDVaiTro = adminRole.IDVaiTro;
                }


                var nhanVien = _mapper.Map<NhanVien>(nhanVienCreateDto);
                nhanVien.IDNhanVien = Guid.NewGuid();
                nhanVien.NgayTao = DateTime.Now;
                nhanVien.NguoiTao = nhanVienCreateDto.IDNguoiTao.ToString();
                nhanVien.TrangThai = nhanVienCreateDto.TrangThai;


                _context.NhanViens.Add(nhanVien);
                await _context.SaveChangesAsync();

                await _context.Entry(nhanVien).Reference(n => n.VaiTro).LoadAsync();

                var response = _mapper.Map<NhanVienResponseDto>(nhanVien);
                return CreatedAtAction(nameof(GetNhanVien), new { id = nhanVien.IDNhanVien }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating employee: {@Dto}", nhanVienCreateDto);
                return StatusCode(500, new
                {
                    message = "Internal server error when creating employee.",
                    error = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }

        // PUT: api/NhanVien/5
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PutNhanVien(Guid id, [FromBody] NhanVienUpdateDto nhanVienUpdateDto)
        {
            _logger.LogInformation("Attempting to update employee with ID: {Id}. DTO: {@Dto}", id, nhanVienUpdateDto);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for employee update (ID: {Id}). Errors: {@ModelStateErrors}",
                    id, ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList());
                return BadRequest(ModelState);
            }

            try
            {

                var nhanVienToUpdate = await _context.NhanViens.FindAsync(id);
                if (nhanVienToUpdate == null)
                {
                    _logger.LogWarning("Employee with ID: {Id} not found for update.", id);
                    return NotFound($"Employee with ID {id} not found.");
                }


                var maNhanVienExists = await _context.NhanViens
                                                     .AnyAsync(nv => nv.MaNhanVien == nhanVienUpdateDto.MaNhanVien && nv.IDNhanVien != id);
                if (maNhanVienExists)
                {
                    _logger.LogWarning("MaNhanVien '{MaNhanVien}' already exists for another employee.", nhanVienUpdateDto.MaNhanVien);
                    return BadRequest($"Mã nhân viên '{nhanVienUpdateDto.MaNhanVien}' đã tồn tại.");
                }


                var currentUserIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(currentUserIdClaim) && id.ToString() == currentUserIdClaim)
                {

                    if (nhanVienToUpdate.TrangThai != nhanVienUpdateDto.TrangThai)
                    {
                        _logger.LogWarning("User with ID {currentUserIdClaim} attempted to change their own status from {oldStatus} to {newStatus}.",
                            currentUserIdClaim, nhanVienToUpdate.TrangThai, nhanVienUpdateDto.TrangThai);
                        return BadRequest("Bạn không thể thay đổi trạng thái của chính mình.");
                    }
                }

                if (nhanVienUpdateDto.IDVaiTro == Guid.Empty || !await _context.VaiTro.AnyAsync(v => v.IDVaiTro == nhanVienUpdateDto.IDVaiTro))
                {
                    var adminRole = await _context.VaiTro.FirstOrDefaultAsync(v => v.TenVaiTro.ToLower() == "admin");
                    if (adminRole == null)
                    {
                        return BadRequest("Không tìm thấy vai trò Admin trong hệ thống. Vui lòng tạo trước.");
                    }
                    nhanVienUpdateDto.IDVaiTro = adminRole.IDVaiTro;
                }

                _mapper.Map(nhanVienUpdateDto, nhanVienToUpdate);
                if (!string.IsNullOrEmpty(nhanVienUpdateDto.MatKhau))
                {
                    nhanVienToUpdate.MatKhau = nhanVienUpdateDto.MatKhau;
                }
                nhanVienToUpdate.LanCapNhatCuoi = DateTime.Now;
                nhanVienToUpdate.NguoiCapNhat = currentUserIdClaim;

                _context.Entry(nhanVienToUpdate).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Employee with ID: {Id} updated successfully.", id);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!NhanVienExists(id))
                    {
                        _logger.LogWarning(ex, "Put: Employee with ID: {Id} not found during update (concurrency check).", id);
                        return NotFound($"Employee with ID {id} not found.");
                    }
                    else
                    {
                        _logger.LogError(ex, "Put: Concurrency error when updating employee with ID: {Id}", id);
                        throw;
                    }
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee with ID: {Id}", id);
                return StatusCode(500, $"Internal server error when updating employee with ID {id}.");
            }
        }





        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteNhanVien(Guid id)
        {
            _logger.LogInformation("Deleting employee with ID: {Id}", id);
            try
            {
                var nhanVien = await _context.NhanViens.FindAsync(id);
                if (nhanVien == null)
                {
                    _logger.LogWarning("Delete: Employee with ID: {Id} not found.", id);
                    return NotFound($"Employee with ID {id} not found.");
                }

                _context.NhanViens.Remove(nhanVien);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Employee with ID: {Id} deleted successfully.", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting employee with ID: {Id}", id);
                return StatusCode(500, $"Internal server error when deleting employee with ID {id}.");
            }
        }

        private bool NhanVienExists(Guid id)
        {
            return _context.NhanViens.Any(e => e.IDNhanVien == id);
        }


        [HttpGet]
        [Route("employee-role-stats")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<object>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEmployeeRoleStats()
        {
            try
            {
                var stats = await _context.NhanViens
                    .Include(nv => nv.VaiTro)
                    .Where(nv => nv.TrangThai)
                    .GroupBy(nv => nv.VaiTro.TenVaiTro)
                    .Select(g => new
                    {
                        RoleName = g.Key,
                        EmployeeCount = g.Count()
                    })
                    .OrderBy(x => x.RoleName)
                    .ToListAsync();

                _logger.LogInformation("Retrieved employee role stats: {StatsCount} roles.", stats.Count);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employee role stats.");
                return StatusCode(500, new { message = "Lỗi khi tải dữ liệu thống kê vai trò nhân viên." });
            }
        }
    }
}
