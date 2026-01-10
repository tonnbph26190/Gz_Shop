using System;
using System.Collections.Generic;

namespace QuanApi.Dtos
{
    public class TaoDonHangDto
    {
        public Guid IDNhanVien { get; set; }
        public Guid? IDKhachHang { get; set; }
        public List<SanPhamDonHangDto> SanPhams { get; set; }
    }

    public class SanPhamDonHangDto
    {
        public Guid IDSanPhamChiTiet { get; set; }
        public int SoLuong { get; set; }
    }
} 