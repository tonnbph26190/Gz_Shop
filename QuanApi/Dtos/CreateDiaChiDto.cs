using System;
using System.ComponentModel.DataAnnotations;

namespace QuanApi.Dtos
{
    public class CreateDiaChiDto
    {
        [Required(ErrorMessage = "Mã địa chỉ là bắt buộc.")]
        [StringLength(50, ErrorMessage = "Mã địa chỉ không được vượt quá 50 ký tự.")]
        public string MaDiaChi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ chi tiết là bắt buộc.")]
        [StringLength(255, ErrorMessage = "Địa chỉ chi tiết không được vượt quá 255 ký tự.")]
        public string DiaChiChiTiet { get; set; } = string.Empty;

        [Required(ErrorMessage = "ID khách hàng là bắt buộc.")]
        public Guid IDKhachHang { get; set; }

        public bool LaMacDinh { get; set; } = false;
        [StringLength(100, ErrorMessage = "Tên người nhận không được vượt quá 100 ký tự.")]
        [RegularExpression("^[A-Za-z\\sÀ-ỹ]+$", ErrorMessage = "Tên người nhận không được chứa số và ký tự đặc biệt.")]
        public string? TenNguoiNhan { get; set; }

        [Phone(ErrorMessage = "Số điện thoại người nhận không hợp lệ.")]
        [StringLength(11, ErrorMessage = "Số điện thoại không được vượt quá 11 ký tự.")]
        [RegularExpression(@"^(0|\+84|84)?[1-9]\d{8}$", ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string? SdtNguoiNhan { get; set; }

        public bool TrangThai { get; set; } = true;
    }
}