using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace QuanApi.Data
{
    public class ChiTietHoaDon
    {
        [Key]
        public Guid IDChiTietHoaDon { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string MaChiTietHoaDon { get; set; }

        [Required]
        public Guid IDHoaDon { get; set; }

        [Required]
        public Guid IDSanPhamChiTiet { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0.")]
        public int SoLuong { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá phải lớn hơn hoặc bằng 0.")]
        public decimal DonGia { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Thành tiền phải lớn hơn hoặc bằng 0.")]
        public decimal ThanhTien { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? NguoiTao { get; set; }

        public DateTime? LanCapNhatCuoi { get; set; }

        [MaxLength(100)]
        public string? NguoiCapNhat { get; set; }

        public bool TrangThai { get; set; } = true;
        [ForeignKey("IDHoaDon")]
        public virtual HoaDon? HoaDon { get; set; }
        [ForeignKey("IDSanPhamChiTiet")]
        public virtual SanPhamChiTiet? SanPhamChiTiet { get; set; }
    }
}
