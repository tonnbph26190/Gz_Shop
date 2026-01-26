using System;

namespace QuanApi.Dtos
{
    public class NhanVienResponseDto
    {
        public Guid IDNhanVien { get; set; }
        public string MaNhanVien { get; set; } = string.Empty;
        public string TenNhanVien { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;


        public DateTime? NgaySinh { get; set; }
        public bool? GioiTinh { get; set; }
        public string? QueQuan { get; set; }
        public string? CCCD { get; set; }

        public Guid IDVaiTro { get; set; }
        public string TenVaiTro { get; set; } = string.Empty;
        public bool TrangThai { get; set; }
        public DateTime NgayTao { get; set; }
        public Guid IDNguoiTao { get; set; }
        public DateTime? LanCapNhatCuoi { get; set; }
        public Guid? IDNguoiCapNhat { get; set; }
    }
}
