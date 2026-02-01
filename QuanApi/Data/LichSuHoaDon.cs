using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanApi.Data
{
    public class LichSuHoaDon
    {
        [Key]   
        public Guid IDLichSuHoaDon { get; set; } = Guid.NewGuid(); 

        public string MaLichSuHoaDon { get; set; } = string.Empty;

        public Guid IDHoaDon { get; set; } 

        public string TrangThai { get; set; } = string.Empty;

        public string? GhiChu { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;

        public string? NguoiTao { get; set; }

        public DateTime? LanCapNhatCuoi { get; set; }

        public string? NguoiCapNhat { get; set; }

        public bool TrangThaiLichSu { get; set; } = true;

        [ForeignKey("IDHoaDon")]
        public virtual HoaDon? HoaDon { get; set; }
    }

}
