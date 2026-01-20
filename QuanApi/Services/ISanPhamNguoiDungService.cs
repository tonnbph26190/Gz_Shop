using BanQuanAu1.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;
using QuanApi.Dtos;
using QuanApi.Repository;
using QuanApi.Repository.IRepository;

namespace QuanApi.Services
{
    // ================= INTERFACE =================
    public interface ISanPhamNguoiDungService
    {
        Task<List<SanPhamKhachHangViewModel>> GetSanPhamChiTietsAsync(
           int pageNumber, int pageSize,
           string? search, int? priceFrom, int? priceTo,
           string? category, string? size, string? color);
        Task<SanPhamKhachHangViewModel?> GetDetailAsync(Guid id);

        Task<object> GetFilterOptionsAsync();
    }

    public class SanPhamNguoiDungService : ISanPhamNguoiDungService
    {
        private readonly GioHangIRepository _gioHangRepo;

        public SanPhamNguoiDungService(GioHangIRepository gioHangRepo)
        {
            _gioHangRepo = gioHangRepo;
        }

        public async Task<List<SanPhamKhachHangViewModel>> GetSanPhamChiTietsAsync(
            int pageNumber, int pageSize,
            string? search, int? priceFrom, int? priceTo,
            string? category, string? size, string? color)
        {
            return await Task.Run(() =>
            {
                var query = _gioHangRepo.ListSPCT(pageNumber, pageSize);

                if (!string.IsNullOrEmpty(search))
                    query = query
                        .Where(x => x.TenSanPham.Contains(search, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                if (priceFrom.HasValue)
                    query = query
                        .Where(x => x.BienThes.Any(b => b.GiaSauGiam >= priceFrom.Value))
                        .ToList();

                if (priceTo.HasValue)
                    query = query
                        .Where(x => x.BienThes.Any(b => b.GiaSauGiam <= priceTo.Value))
                        .ToList();

                if (!string.IsNullOrEmpty(category))
                    query = query
                        .Where(x => x.DanhMuc.Contains(category, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                if (!string.IsNullOrEmpty(size))
                    query = query
                        .Where(x => x.BienThes.Any(b =>
                            b.Size.Contains(size, StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                if (!string.IsNullOrEmpty(color))
                    query = query
                        .Where(x => x.BienThes.Any(b =>
                            b.Mau.Contains(color, StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                // Đảm bảo mỗi sản phẩm có ảnh
                foreach (var sp in query)
                {
                    if (string.IsNullOrEmpty(sp.UrlAnh))
                    {
                        sp.UrlAnh = "/img/default-product.jpg";
                    }
                }

                return query;
            });
        }
        public async Task<SanPhamKhachHangViewModel?> GetDetailAsync(Guid id)
        {
            return await Task.Run(() =>
            {
                return _gioHangRepo.detailSpct(id);
            });
        }

        public async Task<object> GetFilterOptionsAsync()
        {
            return await Task.Run(() =>
            {
                return _gioHangRepo.GetFilterOptions();
            });
        }
    }


}
