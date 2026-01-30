using QuanApi.Data;
using System.ComponentModel.DataAnnotations;

public class KhachHang
{
    [Key]
    public Guid IDKhachHang { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string MaKhachHang { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(255)]
    public string? MatKhau { get; set; }

    [Required]
    [MaxLength(100)]
    public string TenKhachHang { get; set; }

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

    public virtual ICollection<DiaChi>? DiaChis { get; set; }
    public virtual ICollection<GioHang>? GioHangs { get; set; }
    public virtual ICollection<KhachHangPhieuGiam>? KhachHangPhieuGiams { get; set; }
    public virtual ICollection<HoaDon>? HoaDons { get; set; }
    public virtual ICollection<PhongTroChuyen>? PhongTroChuyens { get; set; }
    public virtual ICollection<TinNhan>? TinNhans { get; set; }
}