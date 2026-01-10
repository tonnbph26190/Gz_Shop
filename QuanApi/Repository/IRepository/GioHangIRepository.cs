using QuanApi.Data;
using QuanApi.Dtos;

namespace QuanApi.Repository.IRepository
{
    public interface GioHangIRepository
    {
        List<SanPhamKhachHangViewModel> ListSPCT(int pageNumber, int pageSize);
        SanPhamChiTietDto detailSpct(Guid id); 
        void AddGioHang(Guid iduser, Guid idsp, int soluong);
        GioHang GetByUserId(Guid userId);
        void XoaChiTietGioHang(Guid idgiohang);
        void UpdateChiTietGioHang(Guid idghct, int soluong);
        FilterOptionsDto GetFilterOptions();
    }
}
