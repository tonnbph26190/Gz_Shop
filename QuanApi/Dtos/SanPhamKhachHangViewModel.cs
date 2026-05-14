namespace QuanApi.Dtos
{
    public class SanPhamKhachHangViewModel
    {
        public string TenSanPham { get; set; }
        public string DanhMuc { get; set; }
        public string UrlAnh { get; set; }
        public List<BienTheSanPhamViewModel> BienThes { get; set; }

		public List<string> DanhSachAnh { get; set; } = new();
	}

    public class BienTheSanPhamViewModel
    {
        public Guid IDSanPhamChiTiet { get; set; }
        public string Size { get; set; }
        public string Mau { get; set; }
        public decimal GiaGoc { get; set; }
		public string? AnhDaiDien { get; set; }
		public string HoaTiet { get; set; }

		public decimal GiaSauGiam { get; set; }
        public int SoLuong { get; set; }
    }
}
