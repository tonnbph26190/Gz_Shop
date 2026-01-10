using System;

namespace QuanApi.Dtos
{
    public class PhieuGiamGiaDto
    {
        public Guid IDPhieuGiamGia { get; set; }

        public string MaCode { get; set; } = default!;

        public string TenPhieu { get; set; } = default!;

        public int GiaTriGiam { get; set; }

        public bool TheoPhanTram { get; set; }

        public DateTime NgayBatDau { get; set; }

        public DateTime NgayKetThuc { get; set; }

        public int? SoLuong { get; set; }

        public bool TrangThai { get; set; }

        public DateTime NgayTao { get; set; }

        public string NguoiTao { get; set; } = default!;

        public DateTime? LanCapNhatCuoi { get; set; }

        public string? NguoiCapNhat { get; set; }

        public Guid? IDKhachHang { get; set; }  
    }
}
