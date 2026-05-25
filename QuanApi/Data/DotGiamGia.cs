using System.ComponentModel.DataAnnotations;

namespace QuanApi.Data
{
    public class DotGiamGia
    {
        [Key]
        public Guid IDDotGiamGia { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string MaDot { get; set; }

        [Required]
        [MaxLength(100)]
        public string TenDot { get; set; }

        [Required]
        [Range(1, 90, ErrorMessage = "Phần trăm giảm giá phải từ 1 đến 90.")]
        public int PhanTramGiam { get; set; }

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
        public virtual ICollection<SanPhamChiTiet>? SanPhamChiTiets { get; set; } 
        public virtual ICollection<SanPhamDotGiam>? SanPhamDotGiams { get; set; }
    }
}
