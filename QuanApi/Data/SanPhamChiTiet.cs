using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace QuanApi.Data
{
    public class SanPhamChiTiet
    {
        [Key]
        public Guid IDSanPhamChiTiet { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string MaSPChiTiet { get; set; }

        [Required]
        public Guid IDSanPham { get; set; }

        [Required]
        public Guid IDKichCo { get; set; }

        [Required]
        public Guid IDMauSac { get; set; }

        public Guid? IDDotGiamGia { get; set; }
        public Guid? IDHoaTiet { get; set; }

        [MaxLength(255)]
        public string? QRCode { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá bán phải lớn hơn 0.")]
        public decimal GiaBan { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng không được là số âm.")]
        public int SoLuong { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng đặt chỗ không được là số âm.")]
        public int SoLuongDatCho { get; set; } = 0;

        public string? MoTa { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? NguoiTao { get; set; }

        public DateTime? LanCapNhatCuoi { get; set; }

        [MaxLength(100)]
        public string? NguoiCapNhat { get; set; }

        public bool TrangThai { get; set; } = true;
        [ForeignKey("IDSanPham")]
        public virtual SanPham? SanPham { get; set; }
        [ForeignKey("IDKichCo")]
        public virtual KichCo? KichCo { get; set; }
        [ForeignKey("IDMauSac")]
        public virtual MauSac? MauSac { get; set; }
        [ForeignKey("IDDotGiamGia")]
        public virtual DotGiamGia? DotGiamGia { get; set; }
        
        [ForeignKey("IDHoaTiet")]
        public virtual HoaTiet? HoaTiet { get; set; }

        public virtual ICollection<AnhSanPham>? AnhSanPhams { get; set; }

        public virtual ICollection<ChiTietGioHang>? ChiTietGioHangs { get; set; }

        public virtual ICollection<ChiTietHoaDon>? ChiTietHoaDons { get; set; }

        public virtual ICollection<SanPhamDotGiam>? SanPhamDotGiams { get; set; }
    }
}
