using QuanApi.Data;
using QuanApi.Dtos;
using QuanApi.Repository.IRepository;

namespace QuanApi.Services
{
    public interface IDotGiamGiaService
    {
        Task<PagedResultGeneric<DotGiamGia>> GetAllAsync(DotGiamGiaFilterDto filter);
        Task<DotGiamGia?> GetByIdAsync(Guid id);
        Task<bool> CreateAsync(DotGiamGiaCreateDto dto);
        Task<bool> UpdateAsync(Guid id, DotGiamGiaUpdateDto dto);
        Task<List<object>> GetSanPhamsCuaDotAsync(Guid id);
        Task<(bool HasActive, List<Guid> ProductIds)> CheckActiveDiscountsAsync(List<Guid> productIds);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> UpdateTrangThaiAsync(Guid id, bool trangThai);
    }

    public class DotGiamGiaService : IDotGiamGiaService
    {
        private readonly DotGiamGiaIRepository _repository;
        private readonly ILogger<DotGiamGiaService> _logger;

        public DotGiamGiaService(
            DotGiamGiaIRepository repository,
            ILogger<DotGiamGiaService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<PagedResultGeneric<DotGiamGia>> GetAllAsync(DotGiamGiaFilterDto filter)
        {
            return await _repository.GetDotGiamGia(
                filter.MaDot,
                filter.TenDot,
                filter.PhanTramGiam,
                filter.TuNgay,
                filter.DenNgay,
                filter.TrangThai,
                filter.Page,
                filter.PageSize
            );
        }

        public async Task<DotGiamGia?> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Lay dot giam gia theo ID: {Id}", id);
            return await _repository.GetByIdAsync(id);
        }

        public async Task<bool> CreateAsync(DotGiamGiaCreateDto dto)
        {
            if (dto == null || dto.Dot == null)
            {
                _logger.LogWarning("Du lieu tao dot giam gia khong hop le");
                return false;
            }

            dto.Dot.NgayBatDau = dto.Dot.NgayBatDau.ToUniversalTime();
            dto.Dot.NgayKetThuc = dto.Dot.NgayKetThuc.ToUniversalTime();
            dto.Dot.NgayTao = DateTime.UtcNow;
            dto.Dot.TrangThai = true;

            _logger.LogInformation("Tao dot giam gia: {TenDot}", dto.Dot.TenDot);

            return await _repository.CreateAsync(
                dto.Dot,
                dto.ChiTietIds ?? new List<Guid>()
            );
        }

        public async Task<bool> UpdateAsync(Guid id, DotGiamGiaUpdateDto dto)
        {
            if (dto == null)
            {
                _logger.LogWarning("DTO null khi update DotGiamGia");
                return false;
            }

            if (id != dto.IDDotGiamGia)
            {
                _logger.LogWarning("ID route khong khop body: {RouteId} - {BodyId}", id, dto.IDDotGiamGia);
                return false;
            }

            var dot = new DotGiamGia
            {
                IDDotGiamGia = dto.IDDotGiamGia,
                MaDot = dto.MaDot,
                TenDot = dto.TenDot,
                PhanTramGiam = dto.PhanTramGiam,
                NgayBatDau = dto.NgayBatDau.ToUniversalTime(),
                NgayKetThuc = dto.NgayKetThuc.ToUniversalTime(),
                TrangThai = dto.TrangThai
            };

            _logger.LogInformation(
                "Update trang thai DotGiamGia {Id} => {TrangThai}",
                dot.IDDotGiamGia,
                dot.TrangThai
            );

            return await _repository.UpdateAsync(dot, dto.SanPhamChiTietIds);
        }

        public async Task<List<object>> GetSanPhamsCuaDotAsync(Guid id)
        {
            _logger.LogInformation("Lay danh sach san pham cua dot giam gia: {Id}", id);

            var data = await _repository.GetAllSanPhamChiTietWithSelected(id);
            return data.Cast<object>().ToList();
        }

        public async Task<(bool HasActive, List<Guid> ProductIds)> CheckActiveDiscountsAsync(List<Guid> productIds)
        {
            if (productIds == null || !productIds.Any())
            {
                _logger.LogWarning("Danh sach san pham khong hop le khi check dot giam gia.");
                return (false, new List<Guid>());
            }

            _logger.LogInformation("Check dot giam gia dang hoat dong cho {Count} san pham", productIds.Count);

            var productsWithActiveDiscounts =
                await _repository.GetProductsWithActiveDiscounts(productIds);

            return (productsWithActiveDiscounts.Any(), productsWithActiveDiscounts);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation("Xoa dot giam gia ID: {Id}", id);
            return await _repository.DeleteAsync(id);
        }

        public async Task<bool> UpdateTrangThaiAsync(Guid id, bool trangThai)
        {
            _logger.LogInformation("Cap nhat trang thai dot giam gia ID: {Id} => {TrangThai}", id, trangThai);
            return await _repository.UpdateTrangThaiAsync(id, trangThai);
        }
    }
}
