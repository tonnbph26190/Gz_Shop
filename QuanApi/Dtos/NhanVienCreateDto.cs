using System;
using System.ComponentModel.DataAnnotations;

namespace QuanApi.Dtos
{
    public class NhanVienCreateDto
    {
        [Required(ErrorMessage = "Mã nhân viên là bắt buộc.")]
        [StringLength(50, ErrorMessage = "Mã nhân viên không được vượt quá 50 ký tự.")]
        public string MaNhanVien { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên nhân viên là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Tên nhân viên không được vượt quá 100 ký tự.")]
        [RegularExpression("^[A-Za-z\\sÀ-ỹ]+$", ErrorMessage = "Tên nhân viên không được chứa số và ký tự đặc biệt.")]
        public string TenNhanVien { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [StringLength(50, ErrorMessage = "Email không được vượt quá 50 ký tự.")]
        [RegularExpression(@"^[\w!#$%&'*+\-/=?^_`{|}~]+(\.[\w!#$%&'*+\-/=?^_`{|}~]+)*@((([a-zA-Z0-9-]+\.)+[a-zA-Z]{2,})|((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.){3}([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])))$", ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự và không quá 100 ký tự.")]
        public string MatKhau { get; set; } = string.Empty;
        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
        [StringLength(11, ErrorMessage = "Số điện thoại không được vượt quá 11 ký tự.")]
        [RegularExpression(@"^(0|\+84|84)?[1-9]\d{8}$", ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string SoDienThoai { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngày sinh là bắt buộc.")]
        [CustomValidation(typeof(NhanVienCreateDto), nameof(ValidateAge))]
        public DateTime? NgaySinh { get; set; }

        public bool? GioiTinh { get; set; }

        [StringLength(100, ErrorMessage = "Quê quán không được vượt quá 100 ký tự.")]
        [RegularExpression("^[A-Za-z\\sÀ-ỹ]+$", ErrorMessage = "Tên quê quán không được chứa số và ký tự đặc biệt.")]
        public string? QueQuan { get; set; }

        [StringLength(12, ErrorMessage = "Căn cước công dân phải có 12 ký tự.")]
        [RegularExpression(@"^\d{12}$", ErrorMessage = "Căn cước công dân phải là một chuỗi 12 chữ số.")]
        public string? CCCD { get; set; }

        [Required(ErrorMessage = "ID Vai trò là bắt buộc.")]
        public Guid IDVaiTro { get; set; }

        public bool TrangThai { get; set; } = true;
        public Guid IDNguoiTao { get; set; }

        public static ValidationResult? ValidateAge(DateTime? ngaySinh, ValidationContext context)
        {
            if (ngaySinh.HasValue)
            {
                var today = DateTime.Today;
                var age = today.Year - ngaySinh.Value.Year;
                if (ngaySinh.Value.Date > today.AddYears(-age)) age--;

                if (age < 18)
                {
                    return new ValidationResult("Nhân viên phải trên 18 tuổi!!!", new[] { nameof(NgaySinh) });
                }
            }
            return ValidationResult.Success;
        }
    }
}
