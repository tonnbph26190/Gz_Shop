using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace QuanApi.Data
{
    public class KhachHangPhieuGiam
    {
        [Key]
        public Guid IDKhachHangPhieuGiam { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string MaKhachHangPhieuGiam { get; set; }

        [Required]
        public Guid IDKhachHang { get; set; }

        [Required]
        public Guid IDPhieuGiamGia { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int SoLuong { get; set; } = 1;

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng đã sử dụng không được âm")]
        public int SoLuongDaSuDung { get; set; } = 0;

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? NguoiTao { get; set; }

        public DateTime? LanCapNhatCuoi { get; set; }

        [MaxLength(100)]
        public string? NguoiCapNhat { get; set; }

        public bool TrangThai { get; set; } = true;

        [ForeignKey("IDKhachHang")]
        public virtual KhachHang? KhachHang { get; set; }
        
        // 1-n: PhieuGiamGia -> KhachHangPhieuGiam
        [ForeignKey("IDPhieuGiamGia")]
        public virtual PhieuGiamGia? PhieuGiamGia { get; set; }

        // Property để tính số lượng còn lại
        [NotMapped]
        public int SoLuongConLai => SoLuong - SoLuongDaSuDung;

        // Property để kiểm tra còn sử dụng được không
        [NotMapped]
        public bool ConSuDungDuoc => SoLuongConLai > 0 && TrangThai;
    }
}
