using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QuanApi.Dtos
{
    public class DiaChiDto
    {
        public Guid IDDiaChi { get; set; }

        [DisplayName("Mã địa chỉ")]
        public string MaDiaChi { get; set; } = string.Empty;

        [DisplayName("Địa chỉ chi tiết")]
        public string DiaChiChiTiet { get; set; } = string.Empty;

        public Guid IDKhachHang { get; set; } 

        [DisplayName("Là mặc định")]
        public bool LaMacDinh { get; set; }

        [DisplayName("Tên người nhận")]
        public string? TenNguoiNhan { get; set; }

        [DisplayName("SĐT người nhận")]
        public string? SdtNguoiNhan { get; set; }

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
    }
}