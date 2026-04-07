using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QuanApi.Dtos
{
	public class KhachHangDto
	{
		public Guid IDKhachHang { get; set; }

		[DisplayName("Mã khách hàng")]
		public string MaKhachHang { get; set; } = string.Empty;

		[DisplayName("Tên khách hàng")]
		[Required(ErrorMessage = "Tên khách hàng không được để trống")]
		[RegularExpression(@"^[a-zA-ZÀ-ỹ\s]+$", ErrorMessage = "Tên không được chứa số")]
		public string TenKhachHang { get; set; } = string.Empty;

		[DisplayName("Email")]
		[EmailAddress(ErrorMessage = "Email không đúng định dạng")]
		public string? Email { get; set; }

		[DisplayName("Số điện thoại")]
		[Required(ErrorMessage = "Số điện thoại không được để trống")]
		[RegularExpression(@"^(0|\+84)[0-9]{9}$", ErrorMessage = "Số điện thoại không hợp lệ")]
		public string SoDienThoai { get; set; } = string.Empty;

		[DisplayName("Điểm hiện tại")]
		public int SoDiemHienTai { get; set; }

		[DisplayName("Tổng điểm tích lũy")]
		public int TongDiemTichLuy { get; set; }

		[DisplayName("Ngày tạo")]
		[DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
		public DateTime NgayTao { get; set; }

		[DisplayName("Người tạo")]
		public string? NguoiTao { get; set; }

		[DisplayName("Cập nhật cuối")]
		[DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
		public DateTime? LanCapNhatCuoi { get; set; }

		[DisplayName("Người cập nhật")]
		public string? NguoiCapNhat { get; set; }

		[DisplayName("Trạng thái")]
		public bool TrangThai { get; set; }

		public string? Rank { get; set; }

		public List<DiaChiDto>? DiaChis { get; set; }
	}
}
