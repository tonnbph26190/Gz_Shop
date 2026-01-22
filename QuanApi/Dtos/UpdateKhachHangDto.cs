using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanApi.Dtos
{
    public class UpdateKhachHangDto
    {
        [Required]
        public Guid IDKhachHang { get; set; }

        [Required(ErrorMessage = "Mã khách hàng là bắt buộc.")]
        [StringLength(50, ErrorMessage = "Mã khách hàng không được vượt quá 50 ký tự.")]
        public string MaKhachHang { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên khách hàng là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Tên khách hàng không được vượt quá 100 ký tự.")]
        public string TenKhachHang { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự.")]
        public string? Email { get; set; }

        [StringLength(255, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 đến 255 ký tự.")]
        [DataType(DataType.Password)]
        public string? MatKhau { get; set; }

        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự.")]
        public string SoDienThoai { get; set; } = string.Empty;

        public bool TrangThai { get; set; }

        [StringLength(100, ErrorMessage = "Người cập nhật không được vượt quá 100 ký tự.")]
        public string? NguoiCapNhat { get; set; }

        public List<DiaChiDto>? DiaChis { get; set; }
    }
}