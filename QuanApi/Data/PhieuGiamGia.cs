using System.ComponentModel.DataAnnotations;

namespace QuanApi.Data
{
    public class PhieuGiamGia
    {
        [Key]
        public Guid IDPhieuGiamGia { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string MaCode { get; set; }

        [Required]
        [MaxLength(300)]
        public string TenPhieu { get; set; }

        [Required]
        [Range(0, 100, ErrorMessage = "Phần trăm giảm giá phải từ 0 đến 100%.")]
        public int GiaTriGiam { get; set; } // Phần trăm giảm giá (0-100%)

        [Range(0, double.MaxValue, ErrorMessage = "Giá trị giảm tối đa phải lớn hơn hoặc bằng 0.")]
        public decimal? GiaTriGiamToiDa { get; set; } // Giá trị tiền tối đa được giảm (VNĐ)

        [Range(0, double.MaxValue, ErrorMessage = "Đơn tối thiểu phải lớn hơn hoặc bằng 0.")]
        public decimal? DonToiThieu { get; set; } // Đơn hàng tối thiểu để áp dụng (VNĐ)

        [Required]
        [Range(0, 32767, ErrorMessage = "Số lượng phải lớn hơn hoặc bằng 0.")]
        public short SoLuong { get; set; }

		[Required]
		public bool LaCongKhai { get; set; } = true;

        [Required]
        public DateTime NgayBatDau { get; set; }

        [Required]
        public DateTime NgayKetThuc { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? NguoiTao { get; set; } 

		public DateTime? LanCapNhatCuoi { get; set; }

        [MaxLength(100)]
        public string? NguoiCapNhat { get; set; }

        public bool TrangThai { get; set; } = true;
        public virtual ICollection<KhachHangPhieuGiam>? KhachHangPhieuGiams { get; set; }
        public virtual ICollection<HoaDon>? HoaDons { get; set; }
    }
}
