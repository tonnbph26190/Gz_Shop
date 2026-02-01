using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace BanQuanAu1.Web.Data
{
    public class BanQuanAu1DbContext : DbContext
    {
        public BanQuanAu1DbContext(DbContextOptions<BanQuanAu1DbContext> options)
            : base(options)
        {
        }

        // DbSet cho tất cả các bảng
        public DbSet<DanhMuc> DanhMucs { get; set; }
        public DbSet<ThuongHieu> ThuongHieus { get; set; }
        public DbSet<ChatLieu> ChatLieus { get; set; }
        public DbSet<LoaiOng> LoaiOngs { get; set; }
        public DbSet<KieuDang> KieuDangs { get; set; }
        public DbSet<LungQuan> LungQuans { get; set; }
        public DbSet<KichCo> KichCos { get; set; }
        public DbSet<MauSac> MauSacs { get; set; }
        public DbSet<DotGiamGia> DotGiamGias { get; set; }
        public DbSet<PhieuGiamGia> PhieuGiamGias { get; set; }
        public DbSet<KhachHang> KhachHang { get; set; }
        public DbSet<DiaChi> DiaChis { get; set; }
        public DbSet<KhachHangPhieuGiam> KhachHangPhieuGiams { get; set; }
        public DbSet<NhanVien> NhanViens { get; set; }
        public DbSet<SanPham> SanPhams { get; set; }
        public DbSet<SanPhamChiTiet> SanPhamChiTiets { get; set; }
        public DbSet<AnhSanPham> AnhSanPhams { get; set; }
        public DbSet<GioHang> GioHangs { get; set; }
        public DbSet<ChiTietGioHang> ChiTietGioHangs { get; set; }
        public DbSet<PhuongThucThanhToan> PhuongThucThanhToans { get; set; }
        public DbSet<HoaDon> HoaDons { get; set; }
        public DbSet<ChiTietHoaDon> ChiTietHoaDons { get; set; }
        public DbSet<LichSuHoaDon> LichSuHoaDons { get; set; }
        public DbSet<PhongTroChuyen> PhongTroChuyens { get; set; }
        public DbSet<TinNhan> TinNhans { get; set; }
        public DbSet<SanPhamDotGiam> SanPhamDotGiams { get; set; }
        public DbSet<Banner> Banners { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NhanVien>()
                .HasOne(nv => nv.VaiTro)
                .WithMany(vt => vt.NhanViens)
                .HasForeignKey(nv => nv.IDVaiTro);

            // Seed data cho Vai Trò
            var adminRoleId = Guid.Parse("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d");
            var nhanVienRoleId = Guid.Parse("b2c3d4e5-f6a7-5b6c-9d0e-1f2a3b4c5d6e");
            var khachHangRoleId = Guid.Parse("c3d4e5f6-a7b8-6c7d-0e1f-2a3b4c5d6e7f");

            modelBuilder.Entity<VaiTro>().HasData(
                new VaiTro
                {
                    IDVaiTro = adminRoleId,
                    MaVaiTro = "ADMIN",
                    TenVaiTro = "Quản trị viên",
                    NgayTao = DateTime.UtcNow,
                    NguoiTao = "System",
                    TrangThai = true
                },
                new VaiTro
                {
                    IDVaiTro = nhanVienRoleId,
                    MaVaiTro = "NHANVIEN",
                    TenVaiTro = "Nhân viên",
                    NgayTao = DateTime.UtcNow,
                    NguoiTao = "System",
                    TrangThai = true
                },
                new VaiTro
                {
                    IDVaiTro = khachHangRoleId,
                    MaVaiTro = "KHACHHANG",
                    TenVaiTro = "Khách hàng",
                    NgayTao = DateTime.UtcNow,
                    NguoiTao = "System",
                    TrangThai = true
                }
            );
        }
        public DbSet<QuanApi.Data.HoaTiet> HoaTiet { get; set; } = default!;
        public DbSet<QuanApi.Data.VaiTro> VaiTro { get; set; } = default!;
    }
}