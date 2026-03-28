using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanApi.Data
{
    public class LichSuDiemKhachHang
    {
        [Key]
        public Guid IDLichSuDiemKhachHang { get; set; } = Guid.NewGuid();

        [Required]
        public Guid IDKhachHang { get; set; }

        public Guid? IDHoaDon { get; set; }

        [Required]
        [MaxLength(30)]
        public string LoaiBienDong { get; set; } = "Cong";

        [Required]
        public int SoDiemBienDong { get; set; }

        [Required]
        public int SoDiemTruoc { get; set; }

        [Required]
        public int SoDiemSau { get; set; }

        [MaxLength(255)]
        public string? MoTa { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? NguoiTao { get; set; }

        public bool TrangThai { get; set; } = true;

        [ForeignKey(nameof(IDKhachHang))]
        public virtual KhachHang? KhachHang { get; set; }

        [ForeignKey(nameof(IDHoaDon))]
        public virtual HoaDon? HoaDon { get; set; }
    }
}
