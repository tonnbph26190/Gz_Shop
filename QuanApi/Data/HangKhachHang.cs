using System.ComponentModel.DataAnnotations;

namespace QuanApi.Data
{
    public class HangKhachHang
    {
        [Key]
        public Guid IDHangKhachHang { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string MaHang { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string TenHang { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int DiemTu { get; set; }

        [Range(0, int.MaxValue)]
        public int? DiemDen { get; set; }

        [Range(0, 100)]
        public decimal PhanTramGiamPhiShip { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? NguoiTao { get; set; }

        public DateTime? LanCapNhatCuoi { get; set; }

        [MaxLength(100)]
        public string? NguoiCapNhat { get; set; }

        public bool TrangThai { get; set; } = true;

        public virtual ICollection<KhachHang>? KhachHangs { get; set; }
    }
}
