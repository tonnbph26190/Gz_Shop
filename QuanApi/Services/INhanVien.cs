using AutoMapper;
using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;
using QuanApi.Dtos;

namespace QuanApi.Services
{
    public interface INhanVienService
    {
        Task<PagedResultGeneric<NhanVienResponseDto>> GetPagedEmployeesAsync(NhanVienFilterDto filter);

        Task<NhanVienResponseDto> GetByIdAsync(Guid id);

        Task<NhanVienResponseDto> CreateAsync(NhanVienCreateDto createDto);

        Task UpdateAsync(Guid id, NhanVienUpdateDto updateDto, string currentUserId);

        Task DeleteAsync(Guid id);

        Task<IEnumerable<object>> GetRoleStatsAsync();
    }

    public class NhanVienService : INhanVienService
    {
        private readonly BanQuanAu1DbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<NhanVienService> _logger;

        public NhanVienService(BanQuanAu1DbContext context, IMapper mapper, ILogger<NhanVienService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<PagedResultGeneric<NhanVienResponseDto>> GetPagedEmployeesAsync(NhanVienFilterDto filter)
        {
            var query = _context.NhanViens.Include(n => n.VaiTro).AsQueryable();

            // 1. Filtering logic
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var searchTermLower = filter.SearchTerm.ToLower();
                query = query.Where(nv => nv.MaNhanVien.ToLower().Contains(searchTermLower) ||
                                         nv.TenNhanVien.ToLower().Contains(searchTermLower) ||
                                         nv.Email.ToLower().Contains(searchTermLower));
            }

            if (filter.IDVaiTro.HasValue && filter.IDVaiTro != Guid.Empty)
                query = query.Where(nv => nv.IDVaiTro == filter.IDVaiTro);

            if (filter.TrangThai.HasValue)
                query = query.Where(nv => nv.TrangThai == filter.TrangThai);

            // 2. Sorting logic (Simplified for brevity, keep your switch statement here)
            query = ApplySorting(query, filter.SortBy, filter.SortOrder);

            // 3. Paging
            var totalCount = await query.CountAsync();
            var items = await query.Skip((filter.PageNumber - 1) * filter.PageSize)
                                   .Take(filter.PageSize).ToListAsync();

            return new PagedResultGeneric<NhanVienResponseDto>
            {
                Data = _mapper.Map<List<NhanVienResponseDto>>(items),
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        public async Task<NhanVienResponseDto> GetByIdAsync(Guid id)
        {
            var entity = await _context.NhanViens.Include(n => n.VaiTro)
                .FirstOrDefaultAsync(m => m.IDNhanVien == id);
            return _mapper.Map<NhanVienResponseDto>(entity);
        }

        public async Task<NhanVienResponseDto> CreateAsync(NhanVienCreateDto createDto)
        {
            if (await _context.NhanViens.AnyAsync(nv => nv.Email == createDto.Email))
                throw new ArgumentException($"Email '{createDto.Email}' đã tồn tại.");

            var nhanVien = _mapper.Map<NhanVien>(createDto);
            nhanVien.IDNhanVien = Guid.NewGuid();
            nhanVien.NgayTao = DateTime.UtcNow;

            _context.NhanViens.Add(nhanVien);
            await _context.SaveChangesAsync();

            await _context.Entry(nhanVien).Reference(n => n.VaiTro).LoadAsync();
            return _mapper.Map<NhanVienResponseDto>(nhanVien);
        }

        public async Task UpdateAsync(Guid id, NhanVienUpdateDto updateDto, string currentUserId)
        {
            var existing = await _context.NhanViens.FindAsync(id);
            if (existing == null) throw new KeyNotFoundException("Employee not found");

            // Prevent self-deactivation logic
            if (currentUserId == id.ToString() && existing.TrangThai != updateDto.TrangThai)
                throw new InvalidOperationException("Bạn không thể thay đổi trạng thái của chính mình.");

            _mapper.Map(updateDto, existing);
            existing.LanCapNhatCuoi = DateTime.UtcNow;
            existing.NguoiCapNhat = currentUserId;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _context.NhanViens.FindAsync(id);
            if (entity == null) throw new KeyNotFoundException();
            _context.NhanViens.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<object>> GetRoleStatsAsync()
        {
            return await _context.NhanViens.Include(nv => nv.VaiTro)
                .Where(nv => nv.TrangThai)
                .GroupBy(nv => nv.VaiTro.TenVaiTro)
                .Select(g => new { RoleName = g.Key, EmployeeCount = g.Count() })
                .ToListAsync();
        }

        // Helper for sorting to keep the main method clean
        private IQueryable<NhanVien> ApplySorting(IQueryable<NhanVien> query, string sortBy, string order)
        {
            // Insert your switch case logic here...
            return query.OrderBy(n => n.NgayTao);
        }
    }
}