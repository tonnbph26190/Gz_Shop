using System;

namespace QuanApi.Dtos
{
    public class CapNhatSanPhamDto
    {
        public Guid IDHoaDon { get; set; }
        public Guid IDSanPhamChiTiet { get; set; }
        public int SoLuongMoi { get; set; }
    }
} 