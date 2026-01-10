using System;

namespace QuanApi.Dtos
{
    public class XoaKhoiGioHangDto
    {
        public Guid IDGioHang { get; set; }
        public Guid IDSanPhamChiTiet { get; set; }
        public string? NguoiCapNhat { get; set; }
    }
}
