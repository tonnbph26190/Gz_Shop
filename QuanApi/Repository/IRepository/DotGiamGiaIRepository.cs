using BanQuanAu1.Web.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using QuanApi.Data;
using QuanApi.Dtos;

namespace QuanApi.Repository.IRepository
{
    public interface DotGiamGiaIRepository
    {
        Task<PagedResultGeneric<DotGiamGia>> GetDotGiamGia(string maDot, string tenDot, int? phanTramGiam,
                    DateTime? tuNgay, DateTime? denNgay, string trangThai, int page, int pageSize);

        Task<DotGiamGia> GetByIdAsync(Guid id);

        Task<bool> CreateAsync(DotGiamGia dotGiamGia, List<Guid> selectedChiTietIds);

        Task<bool> UpdateAsync(DotGiamGia dotGiamGia, List<Guid> selectedChiTietIds);
        Task<List<SelectListItem>> GetAllSanPhamChiTietWithSelected(Guid idDot);
        Task<bool> DeleteAsync(Guid id);

        Task<bool> UpdateTrangThaiAsync(Guid id, bool trangThai);
        
        Task<List<Guid>> GetProductsWithActiveDiscounts(List<Guid> productIds);
    }
}
