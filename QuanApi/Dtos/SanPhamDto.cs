namespace QuanApi.Dtos
{
    public class SanPhamDto
    {
        public Guid IDSanPham { get; set; }
        public string MaSanPham { get; set; }
        public string TenSanPham { get; set; }
        public Guid IDChatLieu { get; set; }
        public Guid IDDanhMuc { get; set; }
        public Guid IDThuongHieu { get; set; }
        public Guid IDLoaiOng { get; set; }
        public Guid IDKieuDang { get; set; }
        public Guid IDLungQuan { get; set; }
        public bool CoXepLy { get; set; }
        public bool CoGian { get; set; }
        public bool TrangThai { get; set; }

        public string? TenChatLieu { get; set; }
        public string? TenDanhMuc { get; set; }
        public string? TenThuongHieu { get; set; }
        public string? TenLoaiOng { get; set; }
        public string? TenKieuDang { get; set; }
        public string? TenLungQuan { get; set; }
        
        // Thông tin ảnh sản phẩm
        public string? AnhChinh { get; set; }
        public List<AnhSanPhamDto> DanhSachAnh { get; set; } = new List<AnhSanPhamDto>();
        
        public List<SanPhamChiTietDto> ChiTietSanPhams { get; set; }
    }
}
