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

    public int SoDiemHienTai { get; set; } = 0;

    public int TongDiemTichLuy { get; set; } = 0;

    public Guid? IDHangKhachHang { get; set; }

    public DateTime NgayTao { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? NguoiTao { get; set; }
	[MaxLength(255)]
	public string? AnhDaiDien { get; set; }
	public DateTime? LanCapNhatCuoi { get; set; }

    [MaxLength(100)]
    public string? NguoiCapNhat { get; set; }

    public bool TrangThai { get; set; } = true;

    public virtual ICollection<DiaChi>? DiaChis { get; set; }
    public virtual ICollection<GioHang>? GioHangs { get; set; }
    public virtual ICollection<KhachHangPhieuGiam>? KhachHangPhieuGiams { get; set; }
    public virtual ICollection<HoaDon>? HoaDons { get; set; }
    public virtual HangKhachHang? HangKhachHang { get; set; }
    public virtual ICollection<LichSuDiemKhachHang>? LichSuDiemKhachHangs { get; set; }
    public virtual ICollection<PhongTroChuyen>? PhongTroChuyens { get; set; }
    public virtual ICollection<TinNhan>? TinNhans { get; set; }
}
