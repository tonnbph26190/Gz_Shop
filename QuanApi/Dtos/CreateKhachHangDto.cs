using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanApi.Dtos
{
    public class CreateKhachHangDto
    {
        [Required(ErrorMessage = "Mã khách hàng là bắt buộc.")]
        [StringLength(50, ErrorMessage = "Mã khách hàng không được vượt quá 50 ký tự.")]
        public string MaKhachHang { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên khách hàng là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Tên khách hàng không được vượt quá 100 ký tự.")]
        [RegularExpression("^[A-Za-z\\sÀ-ỹ]+$", ErrorMessage = "Tên khách hàng không được chứa số và ký tự đặc biệt.")]
        public string TenKhachHang { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc.")]
        [StringLength(50, ErrorMessage = "Email không được vượt quá 50 ký tự.")]
        [RegularExpression(@"^[\w!#$%&'*+\-/=?^_`{|}~]+(\.[\w!#$%&'*+\-/=?^_`{|}~]+)*@((([a-zA-Z0-9-]+\.)+[a-zA-Z]{2,})|((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.){3}([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])))$", ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
        [StringLength(255, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 đến 255 ký tự.")]
        [DataType(DataType.Password)]
        public string MatKhau { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
        [StringLength(11, ErrorMessage = "Số điện thoại không được vượt quá 11 ký tự.")]
        [RegularExpression(@"^(0|\+84|84)?[1-9]\d{8}$", ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string SoDienThoai { get; set; } = string.Empty;

        public bool TrangThai { get; set; } = true;


        public List<CreateDiaChiDto>? DiaChis { get; set; }
    }
}
