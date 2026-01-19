using AutoMapper;
using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;
using QuanApi.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanApi.Services
{
    public interface IKhachHangService
    {
        Task<(IEnumerable<KhachHangDto> Data, int TotalCount)> GetAllAsync(
            string? search, int pageNumber, int pageSize, string? sortBy, bool sortAscending);

        Task<KhachHangDto?> GetByIdAsync(Guid id);

        Task<KhachHangDto> CreateAsync(KhachHang dto, string? nguoiTao);
        Task<bool> UpdateAsync(Guid id, KhachHang dto, string? nguoiCapNhat);


        Task<bool> DeleteAsync(Guid id);

        Task<IEnumerable<DiaChiDto>> GetAddressesAsync(Guid id);

        Task<DiaChiDto?> GetDefaultAddressAsync(Guid id);
    }
    public class KhachHangService : IKhachHangService
    {
        private readonly BanQuanAu1DbContext _context;
        private readonly IMapper _mapper;

        public KhachHangService(BanQuanAu1DbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<(IEnumerable<KhachHangDto>, int)> GetAllAsync(
            string? search, int pageNumber, int pageSize, string? sortBy, bool sortAscending)
        {
            var query = _context.KhachHang.Include(kh => kh.DiaChis).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(kh =>
                    kh.MaKhachHang.Contains(search) ||
                    kh.TenKhachHang.Contains(search) ||
                    (kh.Email != null && kh.Email.Contains(search)) ||
                    kh.SoDienThoai.Contains(search));
            }

            query = sortBy?.ToLower() switch
            {
                "makhachhang" => sortAscending ? query.OrderBy(x => x.MaKhachHang) : query.OrderByDescending(x => x.MaKhachHang),
                "tenkhachhang" => sortAscending ? query.OrderBy(x => x.TenKhachHang) : query.OrderByDescending(x => x.TenKhachHang),
                "ngaytao" => sortAscending ? query.OrderBy(x => x.NgayTao) : query.OrderByDescending(x => x.NgayTao),
                _ => query.OrderByDescending(x => x.NgayTao)
            };

            var total = await query.CountAsync();

            var data = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (_mapper.Map<IEnumerable<KhachHangDto>>(data), total);
        }

        public async Task<KhachHangDto?> GetByIdAsync(Guid id)
        {
            var kh = await _context.KhachHang
                .Include(x => x.DiaChis)
                .FirstOrDefaultAsync(x => x.IDKhachHang == id);

            return kh == null ? null : _mapper.Map<KhachHangDto>(kh);
        }

        public async Task<KhachHangDto> CreateAsync(KhachHang dto, string? nguoiTao)
        {
            var kh = _mapper.Map<KhachHang>(dto);
            kh.IDKhachHang = Guid.NewGuid();
            kh.NgayTao = DateTime.UtcNow;
            kh.NguoiTao = nguoiTao ?? "System";
            kh.TrangThai = true;

            if (dto.DiaChis != null)
            {
                kh.DiaChis = dto.DiaChis.Select(d =>
                {
                    var dc = _mapper.Map<DiaChi>(d);
                    dc.IDDiaChi = Guid.NewGuid();
                    dc.IDKhachHang = kh.IDKhachHang;
                    dc.NgayTao = DateTime.UtcNow;
                    dc.NguoiTao = nguoiTao ?? "System";
                    dc.TrangThai = true;
                    return dc;
                }).ToList();
            }

            _context.KhachHang.Add(kh);
            await _context.SaveChangesAsync();

            return _mapper.Map<KhachHangDto>(kh);
        }

        public async Task<bool> UpdateAsync(Guid id, KhachHang dto, string? nguoiCapNhat)
        {
            var kh = await _context.KhachHang
                .Include(x => x.DiaChis)
                .FirstOrDefaultAsync(x => x.IDKhachHang == id);

            if (kh == null) return false;

            _mapper.Map(dto, kh);
            kh.LanCapNhatCuoi = DateTime.UtcNow;
            kh.NguoiCapNhat = nguoiCapNhat ?? "System";

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var kh = await _context.KhachHang.FindAsync(id);
            if (kh == null) return false;

            _context.KhachHang.Remove(kh);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<DiaChiDto>> GetAddressesAsync(Guid id)
        {
            var kh = await _context.KhachHang
                .Include(x => x.DiaChis)
                .FirstOrDefaultAsync(x => x.IDKhachHang == id);

            if (kh == null) return Enumerable.Empty<DiaChiDto>();

            return _mapper.Map<IEnumerable<DiaChiDto>>(kh.DiaChis.Where(x => x.TrangThai));
        }

        public async Task<DiaChiDto?> GetDefaultAddressAsync(Guid id)
        {
            var kh = await _context.KhachHang
                .Include(x => x.DiaChis)
                .FirstOrDefaultAsync(x => x.IDKhachHang == id);

            var dc = kh?.DiaChis?.FirstOrDefault(x => x.TrangThai && x.LaMacDinh);
            return dc == null ? null : _mapper.Map<DiaChiDto>(dc);
        }
    }
}
