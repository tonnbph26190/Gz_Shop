using QuanApi.Dtos;

namespace QuanApi.Controllers;

public class SanPhamDetailDto
{
	public Guid IdSanPham { get; set; }
	public string TenSanPham { get; set; }
	public string TenDanhMuc { get; set; }

	public List<string> DanhSachAnh { get; set; } = new();

	public List<SanPhamChiTietDto> BienThes { get; set; } = new();
}
