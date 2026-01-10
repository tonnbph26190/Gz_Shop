namespace QuanApi.Dtos
{
    public class AnhSanPhamDto
{
    public Guid IDAnhSanPham { get; set; }
    public string MaAnh { get; set; }
    public Guid IDSanPhamChiTiet { get; set; } 
    public string UrlAnh { get; set; }
    public bool LaAnhChinh { get; set; }
    public DateTime NgayTao { get; set; }
    public string? NguoiTao { get; set; }
    public DateTime? LanCapNhatCuoi { get; set; }
    public string? NguoiCapNhat { get; set; }
    public bool TrangThai { get; set; }
    }
}