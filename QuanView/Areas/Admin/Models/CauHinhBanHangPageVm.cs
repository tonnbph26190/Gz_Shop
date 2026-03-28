namespace QuanView.Areas.Admin.Models
{
    public class CauHinhBanHangPageVm
    {
        public CauHinhBanHangConfigVm Config { get; set; } = new();
        public List<HangKhachHangVm> Tiers { get; set; } = new();
    }

    public class CauHinhBanHangConfigVm
    {
        public Guid IDCauHinhBanHang { get; set; }
        public decimal PhiShipMacDinh { get; set; }
        public decimal PhiShipNoiThanh { get; set; }
        public decimal PhiShipNgoaiThanh { get; set; }
        public decimal PhiShipToanQuoc { get; set; }
        public string TinhApDungPhiShip { get; set; } = "Hà Nội";
        public string? DanhSachQuanHuyenNoiThanh { get; set; }
        public string NguonTinhPhiShipMacDinh { get; set; } = "GHN";
        public decimal SoTienTrenMotDiemTich { get; set; }
        public decimal SoTienGiamTrenMotDiem { get; set; }
        public int DiemToiDaSuDungMoiDon { get; set; }
    }

    public class HangKhachHangVm
    {
        public Guid IDHangKhachHang { get; set; }
        public string MaHang { get; set; } = string.Empty;
        public string TenHang { get; set; } = string.Empty;
        public int DiemTu { get; set; }
        public int? DiemDen { get; set; }
        public decimal PhanTramGiamPhiShip { get; set; }
        public bool TrangThai { get; set; }
    }
}
