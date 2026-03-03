using System.ComponentModel.DataAnnotations;

namespace QuanApi.Data
{
    public class KieuDang
    {
        [Key]
        public Guid IDKieuDang { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string MaKieuDang { get; set; }

        [Required]
        [MaxLength(100)]
        public string TenKieuDang { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? NguoiTao { get; set; }

        public DateTime? LanCapNhatCuoi { get; set; }

        [MaxLength(100)]
        public string? NguoiCapNhat { get; set; }

        public bool TrangThai { get; set; } = true;
        public virtual ICollection<SanPham>? SanPhams { get; set; }
    }
}
