using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
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
            var result = await _repository.GetDotGiamGia(
                filter.MaDot,
                filter.TenDot,
                filter.PhanTramGiam,
                filter.TuNgay,
                filter.DenNgay,
                filter.TrangThai,
                filter.Page,
                filter.PageSize
            );

            var now = DateTime.Now;

            foreach (var item in result.Data)
            {
                bool trangThaiMoi =
                    item.NgayBatDau <= now &&
                    item.NgayKetThuc >= now;

                if (item.TrangThai != trangThaiMoi)
                {
                    item.TrangThai = trangThaiMoi;

                    _logger.LogInformation(
                        "Cập nhật trạng thái đợt giảm giá {Id} => {TrangThai}",
                        item.IDDotGiamGia,
                        trangThaiMoi);

                    await _repository.UpdateTrangThaiAsync(
                        item.IDDotGiamGia,
                        trangThaiMoi);
                }
            }

            return result;
        }
        public async Task<DotGiamGia?> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Lấy đợt giảm giá theo ID: {Id}", id);
            return await _repository.GetByIdAsync(id);
        }

        public async Task<bool> CreateAsync(DotGiamGiaCreateDto dto)
        {
            if (dto == null || dto.Dot == null)
            {
                _logger.LogWarning("Dữ liệu tạo đợt giảm giá không hợp lệ");
                return false;
            }

            _logger.LogInformation("Tạo / cập nhật đợt giảm giá: {TenDot}", dto.Dot.TenDot);

            var result = await _repository.CreateAsync(
                dto.Dot,
                dto.ChiTietIds ?? new List<Guid>()
            );

            return result;
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
                _logger.LogWarning("ID route không khớp body: {RouteId} - {BodyId}", id, dto.IDDotGiamGia);
                return false;
            }

            var dot = new DotGiamGia
            {
                IDDotGiamGia = dto.IDDotGiamGia,
                MaDot = dto.MaDot,
                TenDot = dto.TenDot,
                PhanTramGiam = dto.PhanTramGiam,
                NgayBatDau = dto.NgayBatDau,
                NgayKetThuc = dto.NgayKetThuc
            };

            return await _repository.UpdateAsync(dot, dto.SanPhamChiTietIds);
        }

     
        public async Task<List<object>> GetSanPhamsCuaDotAsync(Guid id)
        {
            _logger.LogInformation("Lấy danh sách sản phẩm của đợt giảm giá: {Id}", id);

            var data = await _repository.GetAllSanPhamChiTietWithSelected(id);
            return data.Cast<object>().ToList();
        }
        public async Task<(bool HasActive, List<Guid> ProductIds)> CheckActiveDiscountsAsync(List<Guid> productIds)
        {
            if (productIds == null || !productIds.Any())
            {
                _logger.LogWarning("Danh sách sản phẩm không hợp lệ khi check đợt giảm giá.");
                return (false, new List<Guid>());
            }

            _logger.LogInformation("Check đợt giảm giá đang hoạt động cho {Count} sản phẩm", productIds.Count);

            var productsWithActiveDiscounts =
                await _repository.GetProductsWithActiveDiscounts(productIds);

            return (productsWithActiveDiscounts.Any(), productsWithActiveDiscounts);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            _logger.LogInformation("Xóa đợt giảm giá ID: {Id}", id);
            return await _repository.DeleteAsync(id);
        }

        public async Task<bool> UpdateTrangThaiAsync(Guid id, bool trangThai)
        {
            _logger.LogInformation("Cập nhật trạng thái đợt giảm giá ID: {Id} => {TrangThai}", id, trangThai);
            return await _repository.UpdateTrangThaiAsync(id, trangThai);
        }

    }
}
