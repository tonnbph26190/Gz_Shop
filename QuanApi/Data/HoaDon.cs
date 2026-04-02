using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace QuanApi.Data
{
    public class HoaDon
    {
        [Key]
        public Guid IDHoaDon { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string MaHoaDon { get; set; }

        public Guid? IDKhachHang { get; set; }

        public Guid? IDNhanVien { get; set; }

        public Guid? IDPhieuGiamGia { get; set; }

        [Required]
        public Guid IDPhuongThucThanhToan { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Tổng tiền phải lớn hơn hoặc bằng 0.")]
        public decimal TongTien { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Tiền giảm phải lớn hơn hoặc bằng 0.")]
        public decimal? TienGiam { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Phí vận chuyển phải lớn hơn hoặc bằng 0.")]
        public decimal? PhiVanChuyen { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Phí vận chuyển gốc phải lớn hơn hoặc bằng 0.")]
        public decimal? PhiVanChuyenGoc { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giảm phí vận chuyển phải lớn hơn hoặc bằng 0.")]
        public decimal? SoTienGiamPhiVanChuyen { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Điểm đã dùng phải lớn hơn hoặc bằng 0.")]
        public int DiemDaDung { get; set; } = 0;

        [Range(0, double.MaxValue, ErrorMessage = "Tiền giảm từ điểm phải lớn hơn hoặc bằng 0.")]
        public decimal SoTienGiamTuDiem { get; set; } = 0;

        [Range(0, int.MaxValue, ErrorMessage = "Điểm cộng phải lớn hơn hoặc bằng 0.")]
        public int DiemCong { get; set; } = 0;

        [Required]
        [MaxLength(50)]
        public string TrangThai { get; set; }

		public bool BanTaiQuay { get; set; } = false;

		[MaxLength(100)]
        public string? TenNguoiNhan { get; set; }

        [MaxLength(20)]
        public string? SoDienThoaiNguoiNhan { get; set; }

        [MaxLength(255)]
        public string? DiaChiGiaoHang { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? NguoiTao { get; set; }

        public DateTime? LanCapNhatCuoi { get; set; }

        [MaxLength(100)]
        public string? NguoiCapNhat { get; set; }

        [MaxLength(500)]
        public string? LyDoHuyDon { get; set; }

        public bool TrangThaiHoaDon { get; set; } = true;
        [ForeignKey("IDKhachHang")]
        public virtual KhachHang? KhachHang { get; set; }
        [ForeignKey("IDNhanVien")]
        public virtual NhanVien? NhanVien { get; set; }
        [ForeignKey("IDPhieuGiamGia")]
        public virtual PhieuGiamGia? PhieuGiamGia { get; set; }
        [ForeignKey("IDPhuongThucThanhToan")]
        public virtual PhuongThucThanhToan? PhuongThucThanhToan { get; set; }
        public virtual ICollection<LichSuDiemKhachHang>? LichSuDiemKhachHangs { get; set; }
        public virtual ICollection<ChiTietHoaDon>? ChiTietHoaDons { get; set; }
        public virtual ICollection<LichSuHoaDon>? LichSuHoaDons { get; set; }
    }

}
