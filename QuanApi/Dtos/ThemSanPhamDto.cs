using System;

namespace QuanApi.Dtos
{
    public class ThemSanPhamDto
    {
        public Guid IDHoaDon { get; set; }
        public Guid IDSanPhamChiTiet { get; set; }
        public int SoLuong { get; set; }
    }
} 