using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanApi.Migrations
{
    /// <inheritdoc />
    public partial class _0 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Banners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Link = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Banners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatLieus",
                columns: table => new
                {
                    IDChatLieu = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaChatLieu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenChatLieu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LanCapNhatCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatLieus", x => x.IDChatLieu);
                });

            migrationBuilder.CreateTable(
                name: "DanhMucs",
                columns: table => new
                {
                    IDDanhMuc = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaDanhMuc = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenDanhMuc = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LanCapNhatCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhMucs", x => x.IDDanhMuc);
                });

            migrationBuilder.CreateTable(
                name: "DotGiamGias",
                columns: table => new
                {
                    IDDotGiamGia = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaDot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenDot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhanTramGiam = table.Column<int>(type: "int", nullable: false),
                    NgayBatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayKetThuc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LanCapNhatCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DotGiamGias", x => x.IDDotGiamGia);
                });

            migrationBuilder.CreateTable(
                name: "HoaTiet",
                columns: table => new
                {
                    IDHoaTiet = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaHoaTiet = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenHoaTiet = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LanCapNhatCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoaTiet", x => x.IDHoaTiet);
                });

            migrationBuilder.CreateTable(
                name: "KhachHang",
                columns: table => new
                {
                    IDKhachHang = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaKhachHang = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MatKhau = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TenKhachHang = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SoDienThoai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LanCapNhatCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhachHang", x => x.IDKhachHang);
                });

            migrationBuilder.CreateTable(
                name: "KichCos",
                columns: table => new
                {
                    IDKichCo = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaKichCo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenKichCo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LanCapNhatCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KichCos", x => x.IDKichCo);
                });

            migrationBuilder.CreateTable(
                name: "KieuDangs",
                columns: table => new
                {
                    IDKieuDang = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaKieuDang = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenKieuDang = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LanCapNhatCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KieuDangs", x => x.IDKieuDang);
                });

            migrationBuilder.CreateTable(
                name: "LoaiOngs",
                columns: table => new
                {
                    IDLoaiOng = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaLoaiOng = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenLoaiOng = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LanCapNhatCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoaiOngs", x => x.IDLoaiOng);
                });

            migrationBuilder.CreateTable(
                name: "LungQuans",
                columns: table => new
                {
                    IDLungQuan = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaLungQuan = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenLungQuan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LanCapNhatCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LungQuans", x => x.IDLungQuan);
                });

            migrationBuilder.CreateTable(
                name: "MauSacs",
                columns: table => new
                {
                    IDMauSac = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaMauSac = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenMauSac = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LanCapNhatCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MauSacs", x => x.IDMauSac);
                });

            migrationBuilder.CreateTable(
                name: "PhieuGiamGias",
                columns: table => new
                {
                    IDPhieuGiamGia = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenPhieu = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    GiaTriGiam = table.Column<int>(type: "int", nullable: false),
                    GiaTriGiamToiDa = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DonToiThieu = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SoLuong = table.Column<short>(type: "smallint", nullable: false),
                    LaCongKhai = table.Column<bool>(type: "bit", nullable: false),
                    NgayBatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayKetThuc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LanCapNhatCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuGiamGias", x => x.IDPhieuGiamGia);
                });

            migrationBuilder.CreateTable(
                name: "PhuongThucThanhToans",
                columns: table => new
                {
                    IDPhuongThucThanhToan = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaPhuongThuc = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenPhuongThuc = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LanCapNhatCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhuongThucThanhToans", x => x.IDPhuongThucThanhToan);
                });

            migrationBuilder.CreateTable(
                name: "ThuongHieus",
                columns: table => new
                {
                    IDThuongHieu = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaThuongHieu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenThuongHieu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LanCapNhatCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThuongHieus", x => x.IDThuongHieu);
                });

            migrationBuilder.CreateTable(
                name: "VaiTro",
                columns: table => new
                {
                    IDVaiTro = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaVaiTro = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenVaiTro = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LanCapNhatCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaiTro", x => x.IDVaiTro);
                });

            migrationBuilder.CreateTable(
                name: "DiaChis",
                columns: table => new
                {
                    IDDiaChi = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaDiaChi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IDKhachHang = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiaChiChiTiet = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TinhThanhPho = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    QuanHuyen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhuongXa = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LaMacDinh = table.Column<bool>(type: "bit", nullable: false),
                    TenNguoiNhan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SdtNguoiNhan = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LanCapNhatCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiaChis", x => x.IDDiaChi);
                    table.ForeignKey(
                        name: "FK_DiaChis_KhachHang_IDKhachHang",
                        column: x => x.IDKhachHang,
                        principalTable: "KhachHang",
                        principalColumn: "IDKhachHang",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GioHangs",
                columns: table => new
                {
                    IDGioHang = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaGioHang = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IDKhachHang = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LanCapNhatCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GioHangs", x => x.IDGioHang);
                    table.ForeignKey(
                        name: "FK_GioHangs_KhachHang_IDKhachHang",
                        column: x => x.IDKhachHang,
                        principalTable: "KhachHang",
                        principalColumn: "IDKhachHang");
                });

            migrationBuilder.CreateTable(
                name: "KhachHangPhieuGiams",
                columns: table => new
                {
                    IDKhachHangPhieuGiam = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaKhachHangPhieuGiam = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IDKhachHang = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IDPhieuGiamGia = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    SoLuongDaSuDung = table.Column<int>(type: "int", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LanCapNhatCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhachHangPhieuGiams", x => x.IDKhachHangPhieuGiam);
                    table.ForeignKey(
                        name: "FK_KhachHangPhieuGiams_KhachHang_IDKhachHang",
                        column: x => x.IDKhachHang,
                        principalTable: "KhachHang",
                        principalColumn: "IDKhachHang",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KhachHangPhieuGiams_PhieuGiamGias_IDPhieuGiamGia",
                        column: x => x.IDPhieuGiamGia,
                        principalTable: "PhieuGiamGias",
                        principalColumn: "IDPhieuGiamGia",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SanPhams",
                columns: table => new
                {
                    IDSanPham = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaSanPham = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenSanPham = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IDDanhMuc = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IDThuongHieu = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IDChatLieu = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IDLoaiOng = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IDKieuDang = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IDLungQuan = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CoXepLy = table.Column<bool>(type: "bit", nullable: false),
                    CoGian = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LanCapNhatCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SanPhams", x => x.IDSanPham);
                    table.ForeignKey(
                        name: "FK_SanPhams_ChatLieus_IDChatLieu",
                        column: x => x.IDChatLieu,
                        principalTable: "ChatLieus",
                        principalColumn: "IDChatLieu",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SanPhams_DanhMucs_IDDanhMuc",
                        column: x => x.IDDanhMuc,
                        principalTable: "DanhMucs",
                        principalColumn: "IDDanhMuc",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SanPhams_KieuDangs_IDKieuDang",
                        column: x => x.IDKieuDang,
                        principalTable: "KieuDangs",
                        principalColumn: "IDKieuDang",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SanPhams_LoaiOngs_IDLoaiOng",
                        column: x => x.IDLoaiOng,
                        principalTable: "LoaiOngs",
                        principalColumn: "IDLoaiOng",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SanPhams_LungQuans_IDLungQuan",
                        column: x => x.IDLungQuan,
                        principalTable: "LungQuans",
                        principalColumn: "IDLungQuan",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SanPhams_ThuongHieus_IDThuongHieu",
                        column: x => x.IDThuongHieu,
                        principalTable: "ThuongHieus",
                        principalColumn: "IDThuongHieu",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NhanViens",
                columns: table => new
                {
                    IDNhanVien = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaNhanVien = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenNhanVien = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MatKhau = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    NgaySinh = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GioiTinh = table.Column<bool>(type: "bit", nullable: true),
                    QueQuan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CCCD = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SoDienThoai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LanCapNhatCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false),
                    IDVaiTro = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhanViens", x => x.IDNhanVien);
                    table.ForeignKey(
                        name: "FK_NhanViens_VaiTro_IDVaiTro",
                        column: x => x.IDVaiTro,
                        principalTable: "VaiTro",
                        principalColumn: "IDVaiTro");
                });

            migrationBuilder.CreateTable(
                name: "SanPhamChiTiets",
                columns: table => new
                {
                    IDSanPhamChiTiet = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaSPChiTiet = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IDSanPham = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IDKichCo = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IDMauSac = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IDDotGiamGia = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IDHoaTiet = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    QRCode = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    GiaBan = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LanCapNhatCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SanPhamChiTiets", x => x.IDSanPhamChiTiet);
                    table.ForeignKey(
                        name: "FK_SanPhamChiTiets_DotGiamGias_IDDotGiamGia",
                        column: x => x.IDDotGiamGia,
                        principalTable: "DotGiamGias",
                        principalColumn: "IDDotGiamGia");
                    table.ForeignKey(
                        name: "FK_SanPhamChiTiets_HoaTiet_IDHoaTiet",
                        column: x => x.IDHoaTiet,
                        principalTable: "HoaTiet",
                        principalColumn: "IDHoaTiet");
                    table.ForeignKey(
                        name: "FK_SanPhamChiTiets_KichCos_IDKichCo",
                        column: x => x.IDKichCo,
                        principalTable: "KichCos",
                        principalColumn: "IDKichCo",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SanPhamChiTiets_MauSacs_IDMauSac",
                        column: x => x.IDMauSac,
                        principalTable: "MauSacs",
                        principalColumn: "IDMauSac",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SanPhamChiTiets_SanPhams_IDSanPham",
                        column: x => x.IDSanPham,
                        principalTable: "SanPhams",
                        principalColumn: "IDSanPham",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HoaDons",
                columns: table => new
                {
                    IDHoaDon = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaHoaDon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IDKhachHang = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IDNhanVien = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IDPhieuGiamGia = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IDPhuongThucThanhToan = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TongTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TienGiam = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PhiVanChuyen = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenNguoiNhan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SoDienThoaiNguoiNhan = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DiaChiGiaoHang = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LanCapNhatCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LyDoHuyDon = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TrangThaiHoaDon = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoaDons", x => x.IDHoaDon);
                    table.ForeignKey(
                        name: "FK_HoaDons_KhachHang_IDKhachHang",
                        column: x => x.IDKhachHang,
                        principalTable: "KhachHang",
                        principalColumn: "IDKhachHang");
                    table.ForeignKey(
                        name: "FK_HoaDons_NhanViens_IDNhanVien",
                        column: x => x.IDNhanVien,
                        principalTable: "NhanViens",
                        principalColumn: "IDNhanVien");
                    table.ForeignKey(
                        name: "FK_HoaDons_PhieuGiamGias_IDPhieuGiamGia",
                        column: x => x.IDPhieuGiamGia,
                        principalTable: "PhieuGiamGias",
                        principalColumn: "IDPhieuGiamGia");
                    table.ForeignKey(
                        name: "FK_HoaDons_PhuongThucThanhToans_IDPhuongThucThanhToan",
                        column: x => x.IDPhuongThucThanhToan,
                        principalTable: "PhuongThucThanhToans",
                        principalColumn: "IDPhuongThucThanhToan",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhongTroChuyens",
                columns: table => new
                {
                    IDPhongTroChuyen = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaPhongTroChuyen = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IDKhachHang = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IDNhanVien = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LanCapNhatCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhongTroChuyens", x => x.IDPhongTroChuyen);
                    table.ForeignKey(
                        name: "FK_PhongTroChuyens_KhachHang_IDKhachHang",
                        column: x => x.IDKhachHang,
                        principalTable: "KhachHang",
                        principalColumn: "IDKhachHang",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhongTroChuyens_NhanViens_IDNhanVien",
                        column: x => x.IDNhanVien,
                        principalTable: "NhanViens",
                        principalColumn: "IDNhanVien",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnhSanPhams",
                columns: table => new
                {
                    IDAnhSanPham = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaAnh = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IDSanPhamChiTiet = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UrlAnh = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    LaAnhChinh = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LanCapNhatCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnhSanPhams", x => x.IDAnhSanPham);
                    table.ForeignKey(
                        name: "FK_AnhSanPhams_SanPhamChiTiets_IDSanPhamChiTiet",
                        column: x => x.IDSanPhamChiTiet,
                        principalTable: "SanPhamChiTiets",
                        principalColumn: "IDSanPhamChiTiet",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietGioHangs",
                columns: table => new
                {
                    IDChiTietGioHang = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaChiTietGioHang = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IDGioHang = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IDSanPhamChiTiet = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    GiaBan = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LanCapNhatCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietGioHangs", x => x.IDChiTietGioHang);
                    table.ForeignKey(
                        name: "FK_ChiTietGioHangs_GioHangs_IDGioHang",
                        column: x => x.IDGioHang,
                        principalTable: "GioHangs",
                        principalColumn: "IDGioHang",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiTietGioHangs_SanPhamChiTiets_IDSanPhamChiTiet",
                        column: x => x.IDSanPhamChiTiet,
                        principalTable: "SanPhamChiTiets",
                        principalColumn: "IDSanPhamChiTiet",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SanPhamDotGiams",
                columns: table => new
                {
                    IDSanPhamDotGiam = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaSanPhamDotGiam = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IDSanPhamChiTiet = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IDDotGiamGia = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GiaGoc = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LanCapNhatCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SanPhamDotGiams", x => x.IDSanPhamDotGiam);
                    table.ForeignKey(
                        name: "FK_SanPhamDotGiams_DotGiamGias_IDDotGiamGia",
                        column: x => x.IDDotGiamGia,
                        principalTable: "DotGiamGias",
                        principalColumn: "IDDotGiamGia",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SanPhamDotGiams_SanPhamChiTiets_IDSanPhamChiTiet",
                        column: x => x.IDSanPhamChiTiet,
                        principalTable: "SanPhamChiTiets",
                        principalColumn: "IDSanPhamChiTiet",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietHoaDons",
                columns: table => new
                {
                    IDChiTietHoaDon = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaChiTietHoaDon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IDHoaDon = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IDSanPhamChiTiet = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ThanhTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LanCapNhatCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietHoaDons", x => x.IDChiTietHoaDon);
                    table.ForeignKey(
                        name: "FK_ChiTietHoaDons_HoaDons_IDHoaDon",
                        column: x => x.IDHoaDon,
                        principalTable: "HoaDons",
                        principalColumn: "IDHoaDon",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiTietHoaDons_SanPhamChiTiets_IDSanPhamChiTiet",
                        column: x => x.IDSanPhamChiTiet,
                        principalTable: "SanPhamChiTiets",
                        principalColumn: "IDSanPhamChiTiet",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LichSuHoaDons",
                columns: table => new
                {
                    IDLichSuHoaDon = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaLichSuHoaDon = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IDHoaDon = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LanCapNhatCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrangThaiLichSu = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichSuHoaDons", x => x.IDLichSuHoaDon);
                    table.ForeignKey(
                        name: "FK_LichSuHoaDons_HoaDons_IDHoaDon",
                        column: x => x.IDHoaDon,
                        principalTable: "HoaDons",
                        principalColumn: "IDHoaDon",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TinNhans",
                columns: table => new
                {
                    IDTinNhan = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaTinNhan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IDPhongTroChuyen = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IDKhachHang = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IDNhanVien = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LanCapNhatCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TinNhans", x => x.IDTinNhan);
                    table.ForeignKey(
                        name: "FK_TinNhans_KhachHang_IDKhachHang",
                        column: x => x.IDKhachHang,
                        principalTable: "KhachHang",
                        principalColumn: "IDKhachHang");
                    table.ForeignKey(
                        name: "FK_TinNhans_NhanViens_IDNhanVien",
                        column: x => x.IDNhanVien,
                        principalTable: "NhanViens",
                        principalColumn: "IDNhanVien");
                    table.ForeignKey(
                        name: "FK_TinNhans_PhongTroChuyens_IDPhongTroChuyen",
                        column: x => x.IDPhongTroChuyen,
                        principalTable: "PhongTroChuyens",
                        principalColumn: "IDPhongTroChuyen",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnhSanPhams_IDSanPhamChiTiet",
                table: "AnhSanPhams",
                column: "IDSanPhamChiTiet");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietGioHangs_IDGioHang",
                table: "ChiTietGioHangs",
                column: "IDGioHang");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietGioHangs_IDSanPhamChiTiet",
                table: "ChiTietGioHangs",
                column: "IDSanPhamChiTiet");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietHoaDons_IDHoaDon",
                table: "ChiTietHoaDons",
                column: "IDHoaDon");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietHoaDons_IDSanPhamChiTiet",
                table: "ChiTietHoaDons",
                column: "IDSanPhamChiTiet");

            migrationBuilder.CreateIndex(
                name: "IX_DiaChis_IDKhachHang",
                table: "DiaChis",
                column: "IDKhachHang");

            migrationBuilder.CreateIndex(
                name: "IX_GioHangs_IDKhachHang",
                table: "GioHangs",
                column: "IDKhachHang");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDons_IDKhachHang",
                table: "HoaDons",
                column: "IDKhachHang");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDons_IDNhanVien",
                table: "HoaDons",
                column: "IDNhanVien");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDons_IDPhieuGiamGia",
                table: "HoaDons",
                column: "IDPhieuGiamGia");

            migrationBuilder.CreateIndex(
                name: "IX_HoaDons_IDPhuongThucThanhToan",
                table: "HoaDons",
                column: "IDPhuongThucThanhToan");

            migrationBuilder.CreateIndex(
                name: "IX_KhachHangPhieuGiams_IDKhachHang",
                table: "KhachHangPhieuGiams",
                column: "IDKhachHang");

            migrationBuilder.CreateIndex(
                name: "IX_KhachHangPhieuGiams_IDPhieuGiamGia",
                table: "KhachHangPhieuGiams",
                column: "IDPhieuGiamGia");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuHoaDons_IDHoaDon",
                table: "LichSuHoaDons",
                column: "IDHoaDon");

            migrationBuilder.CreateIndex(
                name: "IX_NhanViens_IDVaiTro",
                table: "NhanViens",
                column: "IDVaiTro");

            migrationBuilder.CreateIndex(
                name: "IX_PhongTroChuyens_IDKhachHang",
                table: "PhongTroChuyens",
                column: "IDKhachHang");

            migrationBuilder.CreateIndex(
                name: "IX_PhongTroChuyens_IDNhanVien",
                table: "PhongTroChuyens",
                column: "IDNhanVien");

            migrationBuilder.CreateIndex(
                name: "IX_SanPhamChiTiets_IDDotGiamGia",
                table: "SanPhamChiTiets",
                column: "IDDotGiamGia");

            migrationBuilder.CreateIndex(
                name: "IX_SanPhamChiTiets_IDHoaTiet",
                table: "SanPhamChiTiets",
                column: "IDHoaTiet");

            migrationBuilder.CreateIndex(
                name: "IX_SanPhamChiTiets_IDKichCo",
                table: "SanPhamChiTiets",
                column: "IDKichCo");

            migrationBuilder.CreateIndex(
                name: "IX_SanPhamChiTiets_IDMauSac",
                table: "SanPhamChiTiets",
                column: "IDMauSac");

            migrationBuilder.CreateIndex(
                name: "IX_SanPhamChiTiets_IDSanPham",
                table: "SanPhamChiTiets",
                column: "IDSanPham");

            migrationBuilder.CreateIndex(
                name: "IX_SanPhamDotGiams_IDDotGiamGia",
                table: "SanPhamDotGiams",
                column: "IDDotGiamGia");

            migrationBuilder.CreateIndex(
                name: "IX_SanPhamDotGiams_IDSanPhamChiTiet",
                table: "SanPhamDotGiams",
                column: "IDSanPhamChiTiet");

            migrationBuilder.CreateIndex(
                name: "IX_SanPhams_IDChatLieu",
                table: "SanPhams",
                column: "IDChatLieu");

            migrationBuilder.CreateIndex(
                name: "IX_SanPhams_IDDanhMuc",
                table: "SanPhams",
                column: "IDDanhMuc");

            migrationBuilder.CreateIndex(
                name: "IX_SanPhams_IDKieuDang",
                table: "SanPhams",
                column: "IDKieuDang");

            migrationBuilder.CreateIndex(
                name: "IX_SanPhams_IDLoaiOng",
                table: "SanPhams",
                column: "IDLoaiOng");

            migrationBuilder.CreateIndex(
                name: "IX_SanPhams_IDLungQuan",
                table: "SanPhams",
                column: "IDLungQuan");

            migrationBuilder.CreateIndex(
                name: "IX_SanPhams_IDThuongHieu",
                table: "SanPhams",
                column: "IDThuongHieu");

            migrationBuilder.CreateIndex(
                name: "IX_TinNhans_IDKhachHang",
                table: "TinNhans",
                column: "IDKhachHang");

            migrationBuilder.CreateIndex(
                name: "IX_TinNhans_IDNhanVien",
                table: "TinNhans",
                column: "IDNhanVien");

            migrationBuilder.CreateIndex(
                name: "IX_TinNhans_IDPhongTroChuyen",
                table: "TinNhans",
                column: "IDPhongTroChuyen");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnhSanPhams");

            migrationBuilder.DropTable(
                name: "Banners");

            migrationBuilder.DropTable(
                name: "ChiTietGioHangs");

            migrationBuilder.DropTable(
                name: "ChiTietHoaDons");

            migrationBuilder.DropTable(
                name: "DiaChis");

            migrationBuilder.DropTable(
                name: "KhachHangPhieuGiams");

            migrationBuilder.DropTable(
                name: "LichSuHoaDons");

            migrationBuilder.DropTable(
                name: "SanPhamDotGiams");

            migrationBuilder.DropTable(
                name: "TinNhans");

            migrationBuilder.DropTable(
                name: "GioHangs");

            migrationBuilder.DropTable(
                name: "HoaDons");

            migrationBuilder.DropTable(
                name: "SanPhamChiTiets");

            migrationBuilder.DropTable(
                name: "PhongTroChuyens");

            migrationBuilder.DropTable(
                name: "PhieuGiamGias");

            migrationBuilder.DropTable(
                name: "PhuongThucThanhToans");

            migrationBuilder.DropTable(
                name: "DotGiamGias");

            migrationBuilder.DropTable(
                name: "HoaTiet");

            migrationBuilder.DropTable(
                name: "KichCos");

            migrationBuilder.DropTable(
                name: "MauSacs");

            migrationBuilder.DropTable(
                name: "SanPhams");

            migrationBuilder.DropTable(
                name: "KhachHang");

            migrationBuilder.DropTable(
                name: "NhanViens");

            migrationBuilder.DropTable(
                name: "ChatLieus");

            migrationBuilder.DropTable(
                name: "DanhMucs");

            migrationBuilder.DropTable(
                name: "KieuDangs");

            migrationBuilder.DropTable(
                name: "LoaiOngs");

            migrationBuilder.DropTable(
                name: "LungQuans");

            migrationBuilder.DropTable(
                name: "ThuongHieus");

            migrationBuilder.DropTable(
                name: "VaiTro");
        }
    }
}
