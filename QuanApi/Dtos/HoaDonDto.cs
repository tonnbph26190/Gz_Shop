using System.ComponentModel.DataAnnotations;

namespace QuanApi.Dtos
{
    public class HoaDonDto
    {
        public Guid IDHoaDon { get; set; }
        public string MaHoaDon { get; set; } = string.Empty;
        public decimal TongTien { get; set; }
        public decimal TienGiam { get; set; }
        public decimal PhiShip { get; set; }
        public decimal? PhiVanChuyen { get; set; } // Thêm trường phí vận chuyển
        public decimal DaTra { get; set; }
        public decimal CanThanhToan { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public string PhuongThucNhan { get; set; } = string.Empty;
        public string LoaiDon { get; set; } = string.Empty;
        public DateTime NgayTao { get; set; }
        public string? TenNguoiNhan { get; set; }
        public string? SoDienThoaiNguoiNhan { get; set; }
        public string? DiaChiGiaoHang { get; set; }
        public List<ChiTietHoaDonDto> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDonDto>();
    }

    public class ChiTietHoaDonDto
    {
        public Guid IDChiTietHoaDon { get; set; }
        public string TenSanPham { get; set; } = string.Empty;
        public int SoLuong { get; set; }
        public decimal GiaBan { get; set; }
        public string AnhSanPham { get; set; } = string.Empty;
    }
} 