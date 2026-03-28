using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanApi.Data
{


    public class NhanVien
    {
        [Key]
        public Guid IDNhanVien { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string MaNhanVien { get; set; }

        [Required]
        [MaxLength(100)]
        public string TenNhanVien { get; set; }
        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(255)]
        public string? MatKhau { get; set; }
        public DateTime? NgaySinh { get; set; }

        public bool? GioiTinh { get; set; }

        [MaxLength(100)]
        public string? QueQuan { get; set; }

        [MaxLength(20)]
        public string? CCCD { get; set; }

        [Required]
        [MaxLength(20)]
        public string SoDienThoai { get; set; }


        public DateTime NgayTao { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? NguoiTao { get; set; }

        public DateTime? LanCapNhatCuoi { get; set; }

        [MaxLength(100)]
        public string? NguoiCapNhat { get; set; }

        public bool TrangThai { get; set; } = true;
        public Guid? IDVaiTro { get; set; }

        [ForeignKey("IDVaiTro")]
        public virtual VaiTro? VaiTro { get; set; }

        public virtual ICollection<HoaDon>? HoaDons { get; set; }

        public virtual ICollection<PhongTroChuyen>? PhongTroChuyens { get; set; }

        public virtual ICollection<TinNhan>? TinNhans { get; set; }
    }
}
