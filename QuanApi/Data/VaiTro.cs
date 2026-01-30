using System.ComponentModel.DataAnnotations;

namespace QuanApi.Data
{
    public class VaiTro
    {
        [Key]
        public Guid IDVaiTro { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(50)]
        public string MaVaiTro { get; set; }

        [Required]
        [StringLength(100)]
        public string TenVaiTro { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string? NguoiTao { get; set; }

        public DateTime? LanCapNhatCuoi { get; set; }

        [StringLength(100)]
        public string? NguoiCapNhat { get; set; }

        public bool TrangThai { get; set; } = true;

        public virtual ICollection<NhanVien> NhanViens { get; set; } = new List<NhanVien>();
    }
}
