using System;

namespace QuanApi.Dtos
{
    public class CapNhatSoLuongGioHangDto
    {
        public Guid IDGioHang { get; set; }
        public Guid IDSanPhamChiTiet { get; set; }
        public int SoLuongMoi { get; set; }
        public string? NguoiCapNhat { get; set; }
    }
}
