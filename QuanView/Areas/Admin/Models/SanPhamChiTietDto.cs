using System.ComponentModel.DataAnnotations;

namespace QuanView.Areas.Admin.Models
{
    public class SanPhamChiTietDto
    {
        public Guid IdSanPhamChiTiet { get; set; }
        public Guid IdSanPham { get; set; }

		[Required(ErrorMessage = "Vui lòng chọn kích cỡ.")]
		public Guid IdKichCo { get; set; }
		[Required(ErrorMessage = "Vui lòng chọn màu sắc.")]
		public Guid IdMauSac { get; set; }
		public Guid IdHoaTiet { get; set; }
		[Range(0, int.MaxValue, ErrorMessage = "Số lượng không hợp lệ.")]
		public int SoLuong { get; set; }
		public int SoLuongVatLy { get; set; }
        public int SoLuongDatCho { get; set; }
        public int SoLuongKhaDung { get; set; }
		[Range(1, double.MaxValue, ErrorMessage = "Giá bán phải lớn hơn 0.")]
		public decimal GiaBan { get; set; }
		public decimal originalPrice { get; set; }
        public decimal price { get; set; }
        public string? MaSPChiTiet { get; set; }
        public string? QrCode { get; set; }
        public string? TenKichCo { get; set; }
        public string? TenMauSac { get; set; }
        public string? TenHoaTiet { get; set; }
        public string? TenSanPham { get; set; } // Thêm trường này
        public string? AnhDaiDien { get; set; } // Thêm trường này
        public bool TrangThai { get; set; } // Thêm trường này
        
        // Thêm danh sách ảnh
        public List<AnhSanPhamDto> DanhSachAnh { get; set; } = new List<AnhSanPhamDto>();

		public bool IsDeleted { get; set; } = false;
	}
}
