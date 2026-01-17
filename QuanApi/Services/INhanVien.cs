using AutoMapper;
using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;
using QuanApi.Dtos;

namespace QuanApi.Services
{
    public interface INhanVienService
    {
        Task<PagedResultGeneric<NhanVienResponseDto>> GetNhanViensAsync(NhanVienFilterDto filter);
        Task<NhanVienResponseDto> GetNhanVienByIdAsync(Guid id);
        Task<NhanVienResponseDto> CreateNhanVienAsync(NhanVienCreateDto createDto);
        Task UpdateNhanVienAsync(Guid id, NhanVienUpdateDto updateDto, string currentUserId);
        Task<bool> DeleteNhanVienAsync(Guid id);
        Task<IEnumerable<object>> GetEmployeeRoleStatsAsync();
    }
    public class NhanVienService : INhanVienService
    {
        private readonly BanQuanAu1DbContext _context;
        private readonly IMapper _mapper;

        public NhanVienService(BanQuanAu1DbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PagedResultGeneric<NhanVienResponseDto>> GetNhanViensAsync(NhanVienFilterDto filter)
        {
            var query = _context.NhanViens.Include(n => n.VaiTro).AsQueryable();

            // 1. Filtering logic
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                query = query.Where(nv => nv.MaNhanVien.Contains(filter.SearchTerm) ||
                                         nv.TenNhanVien.Contains(filter.SearchTerm) ||
                                         nv.Email.Contains(filter.SearchTerm));
            }

            if (filter.IDVaiTro.HasValue && filter.IDVaiTro.Value != Guid.Empty)
                query = query.Where(nv => nv.IDVaiTro == filter.IDVaiTro.Value);

            // 2. Sorting logic (Simplified for brevity, keep your switch statement here)
            query = ApplySorting(query, filter);

            // 3. Paging
            var totalCount = await query.CountAsync();
            var items = await query.Skip((filter.PageNumber - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync();

            return new PagedResultGeneric<NhanVienResponseDto>
            {
                Data = _mapper.Map<List<NhanVienResponseDto>>(items),
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        public async Task<NhanVienResponseDto> GetNhanVienByIdAsync(Guid id)
        {
            var nhanVien = await _context.NhanViens.Include(n => n.VaiTro).FirstOrDefaultAsync(m => m.IDNhanVien == id);
            return _mapper.Map<NhanVienResponseDto>(nhanVien);
        }

        public async Task<NhanVienResponseDto> CreateNhanVienAsync(NhanVienCreateDto createDto)
        {
            if (await _context.NhanViens.AnyAsync(nv => nv.Email == createDto.Email))
                throw new InvalidOperationException($"Email '{createDto.Email}' đã tồn tại.");

            var nhanVien = _mapper.Map<NhanVien>(createDto);
            nhanVien.IDNhanVien = Guid.NewGuid();
            nhanVien.NgayTao = DateTime.Now;

            _context.NhanViens.Add(nhanVien);
            await _context.SaveChangesAsync();

            await _context.Entry(nhanVien).Reference(n => n.VaiTro).LoadAsync();
            return _mapper.Map<NhanVienResponseDto>(nhanVien);
        }

        public async Task UpdateNhanVienAsync(Guid id, NhanVienUpdateDto updateDto, string currentUserId)
        {
            var existing = await _context.NhanViens.FindAsync(id);
            if (existing == null) throw new KeyNotFoundException("Employee not found");

            if (id.ToString() == currentUserId && existing.TrangThai != updateDto.TrangThai)
                throw new InvalidOperationException("Bạn không thể thay đổi trạng thái của chính mình.");

            _mapper.Map(updateDto, existing);
            existing.LanCapNhatCuoi = DateTime.Now;
            existing.NguoiCapNhat = currentUserId;

            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteNhanVienAsync(Guid id)
        {
            var nhanVien = await _context.NhanViens.FindAsync(id);

            if (nhanVien == null)
                return false;

            _context.NhanViens.Remove(nhanVien);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<object>> GetEmployeeRoleStatsAsync()
        {
            return await _context.NhanViens
                .Include(nv => nv.VaiTro)
                .Where(nv => nv.TrangThai)
                .GroupBy(nv => nv.VaiTro.TenVaiTro)
                .Select(g => new { RoleName = g.Key, EmployeeCount = g.Count() })
                .ToListAsync();
        }

        // Helper for sorting
        private IQueryable<NhanVien> ApplySorting(IQueryable<NhanVien> query, NhanVienFilterDto filter)
        { /* Paste your existing switch-case logic here */ return query; }
    }
}
