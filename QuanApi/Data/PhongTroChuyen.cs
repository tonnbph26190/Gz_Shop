using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanApi.Data
{
    public class PhongTroChuyen
    {
        [Key]
        public Guid IDPhongTroChuyen { get; set; } = Guid.NewGuid(); // Tự động sinh GUID

        public string MaPhongTroChuyen { get; set; } = string.Empty;

        public Guid IDKhachHang { get; set; }

        public Guid IDNhanVien { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;

        public string? NguoiTao { get; set; }

        public DateTime? LanCapNhatCuoi { get; set; }

        public string? NguoiCapNhat { get; set; }

        public bool TrangThai { get; set; } = true;
        [ForeignKey("IDKhachHang")]
        public virtual KhachHang? KhachHang { get; set; }
        [ForeignKey("IDNhanVien")]
        public virtual NhanVien? NhanVien { get; set; }
        public virtual ICollection<TinNhan>? TinNhans { get; set; }
    }

}
