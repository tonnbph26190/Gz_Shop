using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace QuanApi.Data
{
    public class GioHang
    {
        [Key]
        public Guid IDGioHang { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string MaGioHang { get; set; }

        public Guid? IDKhachHang { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? NguoiTao { get; set; }

        public DateTime? LanCapNhatCuoi { get; set; }

        [MaxLength(100)]
        public string? NguoiCapNhat { get; set; }

        public bool TrangThai { get; set; } = true;
        [ForeignKey("IDKhachHang")]
        public virtual KhachHang? KhachHang { get; set; }
        public virtual ICollection<ChiTietGioHang>? ChiTietGioHangs { get; set; }
    }
}
