using System.ComponentModel.DataAnnotations;

namespace QuanApi.Data
{
    public class CauHinhBanHang
    {
        [Key]
        public Guid IDCauHinhBanHang { get; set; } = Guid.NewGuid();

        [Range(0, double.MaxValue)]
        public decimal PhiShipMacDinh { get; set; } = 50000;

        [Range(0, double.MaxValue)]
        public decimal PhiShipNoiThanh { get; set; } = 20000;

        [Range(0, double.MaxValue)]
        public decimal PhiShipNgoaiThanh { get; set; } = 35000;

        [Range(0, double.MaxValue)]
        public decimal PhiShipToanQuoc { get; set; } = 50000;

        [MaxLength(100)]
        public string TinhApDungPhiShip { get; set; } = "Hà Nội";

        [MaxLength(2000)]
        public string? DanhSachQuanHuyenNoiThanh { get; set; } =
            "Ba Đình,Hoàn Kiếm,Tây Hồ,Long Biên,Cầu Giấy,Đống Đa,Hai Bà Trưng,Hoàng Mai,Thanh Xuân,Nam Từ Liêm,Bắc Từ Liêm,Hà Đông";

        [MaxLength(20)]
        public string NguonTinhPhiShipMacDinh { get; set; } = "GHN";

        [Range(1, double.MaxValue)]
        public decimal SoTienTrenMotDiemTich { get; set; } = 10000;

        [Range(1, double.MaxValue)]
        public decimal SoTienGiamTrenMotDiem { get; set; } = 1000;

        [Range(0, int.MaxValue)]
        public int DiemToiDaSuDungMoiDon { get; set; } = 0;

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? NguoiTao { get; set; }

        public DateTime? LanCapNhatCuoi { get; set; }

        [MaxLength(100)]
        public string? NguoiCapNhat { get; set; }

        public bool TrangThai { get; set; } = true;
    }
}
