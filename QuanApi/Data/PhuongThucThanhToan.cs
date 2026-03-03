using System.ComponentModel.DataAnnotations;

namespace QuanApi.Data
{
    public class PhuongThucThanhToan
    {
        [Key]
        public Guid IDPhuongThucThanhToan { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string MaPhuongThuc { get; set; }

        [Required]
        [MaxLength(100)]
        public string TenPhuongThuc { get; set; }

        public string? MoTa { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? NguoiTao { get; set; }

        public DateTime? LanCapNhatCuoi { get; set; }

        [MaxLength(100)]
        public string? NguoiCapNhat { get; set; }

        public bool TrangThai { get; set; } = true;
        public virtual ICollection<HoaDon>? HoaDons { get; set; }
    }
}
