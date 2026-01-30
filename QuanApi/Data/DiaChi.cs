using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace QuanApi.Data
{
    public class DiaChi
    {
        [Key]
        public Guid IDDiaChi { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string MaDiaChi { get; set; }

        [Required]
        public Guid IDKhachHang { get; set; }

        [Required]
        [MaxLength(255)]
        public string DiaChiChiTiet { get; set; }

        [Required]
        public bool LaMacDinh { get; set; } = false;

        [MaxLength(100)]
        public string? TenNguoiNhan { get; set; }

        [MaxLength(20)]
        public string? SdtNguoiNhan { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? NguoiTao { get; set; }

        public DateTime? LanCapNhatCuoi { get; set; }

        [MaxLength(100)]
        public string? NguoiCapNhat { get; set; }

        public bool TrangThai { get; set; } = true;

        [ForeignKey("IDKhachHang")]
        public virtual KhachHang? KhachHang { get; set; }
    }
}
