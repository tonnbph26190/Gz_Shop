using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace QuanApi.Data
{
    public class SanPham
    {
        [Key]
        public Guid IDSanPham { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string MaSanPham { get; set; }

        [Required]
        [MaxLength(255)]
        public string TenSanPham { get; set; }

        [Required]
        public Guid IDDanhMuc { get; set; }

        [Required]
        public Guid IDThuongHieu { get; set; }

        [Required]
        public Guid IDChatLieu { get; set; }

        [Required]
        public Guid IDLoaiOng { get; set; }

        [Required]
        public Guid IDKieuDang { get; set; }

        [Required]
        public Guid IDLungQuan { get; set; }

        [Required]
        public bool CoXepLy { get; set; }

        [Required]
        public bool CoGian { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? NguoiTao { get; set; }

        public DateTime? LanCapNhatCuoi { get; set; }

        [MaxLength(100)]
        public string? NguoiCapNhat { get; set; }

        public bool TrangThai { get; set; } = true;
        [ForeignKey("IDDanhMuc")]
        public virtual DanhMuc? DanhMuc { get; set; }

        [ForeignKey("IDThuongHieu")]
        public virtual ThuongHieu? ThuongHieu { get; set; }
        [ForeignKey("IDChatLieu")]
        public virtual ChatLieu? ChatLieu { get; set; }
        [ForeignKey("IDLoaiOng")]
        public virtual LoaiOng? LoaiOng { get; set; }
        [ForeignKey("IDKieuDang")]
        public virtual KieuDang? KieuDang { get; set; }
        [ForeignKey("IDLungQuan")]
        public virtual LungQuan? LungQuan { get; set; }

        public virtual ICollection<SanPhamChiTiet>? SanPhamChiTiets { get; set; }
    }
}
