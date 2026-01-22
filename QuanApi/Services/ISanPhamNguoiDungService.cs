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
        List<SanPhamKhachHangViewModel> GetSanPhamChiTietsAsync(
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

        public List<SanPhamKhachHangViewModel> GetSanPhamChiTietsAsync(
          int pageNumber, int pageSize,
          string? search, int? priceFrom, int? priceTo,
          string? category, string? size, string? color)
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
                    .Where(x => x.BienThes.Any(b => b.Size.Contains(size, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

            if (!string.IsNullOrEmpty(color))
                query = query
                    .Where(x => x.BienThes.Any(b => b.Mau.Contains(color, StringComparison.OrdinalIgnoreCase)))
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
        }
        public async Task<SanPhamKhachHangViewModel?> GetDetailAsync(Guid id)
        {
            var dto = await Task.Run(() => _gioHangRepo.detailSpct(id));

            if (dto == null) return null;

            return new SanPhamKhachHangViewModel
            {
                TenSanPham = dto.TenSanPham,
                DanhMuc = dto.TenDanhMuc,

                UrlAnh = string.IsNullOrEmpty(dto.AnhDaiDien)
                            ? "/img/default-product.jpg"
                            : dto.AnhDaiDien,

                BienThes = new List<BienTheSanPhamViewModel>
        {
            new BienTheSanPhamViewModel
            {
                IDSanPhamChiTiet = dto.IdSanPhamChiTiet,
                Size = dto.TenKichCo,
                Mau = dto.TenMauSac,
                GiaGoc = dto.originalPrice,   // map đúng
                GiaSauGiam = dto.price,       // map đúng
                SoLuong = dto.SoLuong
            }
        }
            };
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
