using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuanApi.Data;
using QuanApi.Services;
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
        public DbSet<LichSuDiemKhachHang> LichSuDiemKhachHangs { get; set; }
        public DbSet<PhongTroChuyen> PhongTroChuyens { get; set; }
        public DbSet<TinNhan> TinNhans { get; set; }
        public DbSet<SanPhamDotGiam> SanPhamDotGiams { get; set; }
        public DbSet<Banner> Banners { get; set; }
        public DbSet<BannerSanPham> BannerSanPhams { get; set; }
        public DbSet<CauHinhBanHang> CauHinhBanHangs { get; set; }
        public DbSet<HangKhachHang> HangKhachHangs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NhanVien>()
                .HasOne(nv => nv.VaiTro)
                .WithMany(vt => vt.NhanViens)
                .HasForeignKey(nv => nv.IDVaiTro);

            modelBuilder.Entity<KhachHang>()
                .HasOne(kh => kh.HangKhachHang)
                .WithMany(h => h.KhachHangs)
                .HasForeignKey(kh => kh.IDHangKhachHang)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<LichSuDiemKhachHang>()
                .HasOne(ls => ls.KhachHang)
                .WithMany(kh => kh.LichSuDiemKhachHangs)
                .HasForeignKey(ls => ls.IDKhachHang)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LichSuDiemKhachHang>()
                .HasOne(ls => ls.HoaDon)
                .WithMany(h => h.LichSuDiemKhachHangs)
                .HasForeignKey(ls => ls.IDHoaDon)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<BannerSanPham>()
                .HasOne(bs => bs.Banner)
                .WithMany(b => b.BannerSanPhams)
                .HasForeignKey(bs => bs.BannerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BannerSanPham>()
                .HasOne(bs => bs.SanPham)
                .WithMany(sp => sp.BannerSanPhams)
                .HasForeignKey(bs => bs.IDSanPham)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BannerSanPham>()
                .HasIndex(bs => new { bs.BannerId, bs.IDSanPham })
                .IsUnique();

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

            var defaultConfigId = Guid.Parse("8f3aa6f2-0608-4f47-a406-5de9ef3366d2");
            modelBuilder.Entity<CauHinhBanHang>().HasData(new CauHinhBanHang
            {
                IDCauHinhBanHang = defaultConfigId,
                PhiShipMacDinh = 50000,
                PhiShipNoiThanh = 20000,
                PhiShipNgoaiThanh = 35000,
                PhiShipToanQuoc = 50000,
                TinhApDungPhiShip = "Hà Nội",
                DanhSachQuanHuyenNoiThanh = "Ba Đình,Hoàn Kiếm,Tây Hồ,Long Biên,Cầu Giấy,Đống Đa,Hai Bà Trưng,Hoàng Mai,Thanh Xuân,Nam Từ Liêm,Bắc Từ Liêm,Hà Đông",
                NguonTinhPhiShipMacDinh = ShippingFeeSources.Config,
                SoTienTrenMotDiemTich = 10000,
                SoTienGiamTrenMotDiem = 1000,
                DiemToiDaSuDungMoiDon = 0,
                NgayTao = DateTime.UtcNow,
                NguoiTao = "System",
                TrangThai = true
            });

            modelBuilder.Entity<HangKhachHang>().HasData(
                new HangKhachHang
                {
                    IDHangKhachHang = Guid.Parse("d58c8c83-0f0a-48a8-a2dd-c7ac8f668f69"),
                    MaHang = "BRONZE",
                    TenHang = "Đồng",
                    DiemTu = 0,
                    DiemDen = 499,
                    PhanTramGiamPhiShip = 0,
                    NgayTao = DateTime.UtcNow,
                    NguoiTao = "System",
                    TrangThai = true
                },
                new HangKhachHang
                {
                    IDHangKhachHang = Guid.Parse("49cb8a18-d15d-4df5-97be-f98f6ef88ca4"),
                    MaHang = "SILVER",
                    TenHang = "Bạc",
                    DiemTu = 500,
                    DiemDen = 1499,
                    PhanTramGiamPhiShip = 10,
                    NgayTao = DateTime.UtcNow,
                    NguoiTao = "System",
                    TrangThai = true
                },
                new HangKhachHang
                {
                    IDHangKhachHang = Guid.Parse("b72f6741-bfd9-4c29-8aeb-6f4f2b8cbf3d"),
                    MaHang = "GOLD",
                    TenHang = "Vàng",
                    DiemTu = 1500,
                    DiemDen = 2999,
                    PhanTramGiamPhiShip = 20,
                    NgayTao = DateTime.UtcNow,
                    NguoiTao = "System",
                    TrangThai = true
                },
                new HangKhachHang
                {
                    IDHangKhachHang = Guid.Parse("38fbce12-fc6f-4d1f-badf-fc2b70f6c396"),
                    MaHang = "PLATINUM",
                    TenHang = "Bạch kim",
                    DiemTu = 3000,
                    DiemDen = null,
                    PhanTramGiamPhiShip = 30,
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