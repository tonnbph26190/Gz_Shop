using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace QuanApi.Data
{
    public class AnhSanPham
    {
        [Key]
        public Guid IDAnhSanPham { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string MaAnh { get; set; }

        [Required]
        public Guid IDSanPhamChiTiet { get; set; }

        [Required]
        [MaxLength(255)]
        public string UrlAnh { get; set; }

        [Required]
        public bool LaAnhChinh { get; set; } = false;

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? NguoiTao { get; set; }

        public DateTime? LanCapNhatCuoi { get; set; }

        [MaxLength(100)]
        public string? NguoiCapNhat { get; set; }

        public bool TrangThai { get; set; } = true;
        [ForeignKey("IDSanPhamChiTiet")]
        public virtual SanPhamChiTiet? SanPhamChiTiet { get; set; }
    }
}
