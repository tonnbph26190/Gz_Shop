using System;
using System.ComponentModel.DataAnnotations;

namespace QuanApi.Dtos
{
    public class TaoDiaChiDto
    {
        [Required(ErrorMessage = "ID khách hàng là bắt buộc.")]
        public Guid IDKhachHang { get; set; }

        [Required(ErrorMessage = "Địa chỉ chi tiết là bắt buộc.")]
        [StringLength(255, ErrorMessage = "Địa chỉ chi tiết không được vượt quá 255 ký tự.")]
        public string DiaChiChiTiet { get; set; } = string.Empty;

        public bool LaMacDinh { get; set; } = false;

        [StringLength(100, ErrorMessage = "Tên người nhận không được vượt quá 100 ký tự.")]
        public string? TenNguoiNhan { get; set; }

        [Phone(ErrorMessage = "Số điện thoại người nhận không hợp lệ.")]
        [StringLength(20, ErrorMessage = "Số điện thoại người nhận không được vượt quá 20 ký tự.")]
        public string? SdtNguoiNhan { get; set; }
    }
} 