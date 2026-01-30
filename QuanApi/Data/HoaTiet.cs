using System.ComponentModel.DataAnnotations;

namespace QuanApi.Data
{
    public class HoaTiet
    {
        [Key]
        public Guid IDHoaTiet { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string MaHoaTiet { get; set; } // Ví dụ: HT-CARO, HT-TRON

        [Required]
        [MaxLength(100)]
        public string TenHoaTiet { get; set; } // Ví dụ: Caro, Trơn, Sọc dọc

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? NguoiTao { get; set; }

        public DateTime? LanCapNhatCuoi { get; set; }

        [MaxLength(100)]
        public string? NguoiCapNhat { get; set; }

        public bool TrangThai { get; set; } = true;

        // Liên kết tới sản phẩm chi tiết
        public virtual ICollection<SanPhamChiTiet>? SanPhamChiTiets { get; set; }
    }
}
