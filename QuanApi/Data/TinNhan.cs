using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanApi.Data
{
    public class TinNhan
    {
        [Key]
        public Guid IDTinNhan { get; set; } = Guid.NewGuid();
        public string MaTinNhan { get; set; } = string.Empty;

        public Guid IDPhongTroChuyen { get; set; }

        public Guid? IDKhachHang { get; set; }
        public Guid? IDNhanVien { get; set; }

        public string NoiDung { get; set; } = string.Empty;

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;
        public string? NguoiTao { get; set; }
        public DateTime? LanCapNhatCuoi { get; set; }
        public string? NguoiCapNhat { get; set; }
        public bool TrangThai { get; set; } = true;
        [ForeignKey("IDPhongTroChuyen")]
        public virtual PhongTroChuyen? PhongTroChuyen { get; set; }
        [ForeignKey("IDKhachHang")]
        public virtual KhachHang? KhachHang { get; set; }
        [ForeignKey("IDNhanVien")]
        public virtual NhanVien? NhanVien { get; set; }
    }

}
