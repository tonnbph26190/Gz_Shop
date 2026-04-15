using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace QuanApi.Data
{
    public class ChiTietGioHang
    {
        [Key]
        public Guid IDChiTietGioHang { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string MaChiTietGioHang { get; set; }

        [Required]
        public Guid IDGioHang { get; set; }

        [Required]
        public Guid IDSanPhamChiTiet { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0.")]
        public int SoLuong { get; set; }
        public int SoLuongDatCho { get; set; } = 0;
        public decimal GiaBan { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? NguoiTao { get; set; }

        public DateTime? LanCapNhatCuoi { get; set; }

        [MaxLength(100)]
        public string? NguoiCapNhat { get; set; }

        public bool TrangThai { get; set; } = true;
        [ForeignKey("IDGioHang")]
        public virtual GioHang? GioHang { get; set; }
        [ForeignKey("IDSanPhamChiTiet")]
        public virtual SanPhamChiTiet? SanPhamChiTiet { get; set; }
    }
}
