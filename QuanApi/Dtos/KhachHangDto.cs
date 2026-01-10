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
        public string TenKhachHang { get; set; } = string.Empty;

        [DisplayName("Email")]
        public string? Email { get; set; }

        [DisplayName("Số điện thoại")]
        public string SoDienThoai { get; set; } = string.Empty;

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

        public List<DiaChiDto>? DiaChis { get; set; }
    }
}
