using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace QuanApi.Migrations
{
    /// <inheritdoc />
    public partial class AddLoyaltyShippingConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "IDHangKhachHang",
                table: "KhachHang",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SoDiemHienTai",
                table: "KhachHang",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TongDiemTichLuy",
                table: "KhachHang",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DiemCong",
                table: "HoaDons",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DiemDaDung",
                table: "HoaDons",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PhiVanChuyenGoc",
                table: "HoaDons",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SoTienGiamPhiVanChuyen",
                table: "HoaDons",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SoTienGiamTuDiem",
                table: "HoaDons",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "CauHinhBanHangs",
                columns: table => new
                {
                    IDCauHinhBanHang = table.Column<Guid>(type: "uuid", nullable: false),
                    DanhSachQuanHuyenNoiThanh = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DiemToiDaSuDungMoiDon = table.Column<int>(type: "integer", nullable: false),
                    LanCapNhatCuoi = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NguoiCapNhat = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NguoiTao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NguonTinhPhiShipMacDinh = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PhiShipMacDinh = table.Column<decimal>(type: "numeric", nullable: false),
                    PhiShipNgoaiThanh = table.Column<decimal>(type: "numeric", nullable: false),
                    PhiShipNoiThanh = table.Column<decimal>(type: "numeric", nullable: false),
                    PhiShipToanQuoc = table.Column<decimal>(type: "numeric", nullable: false),
                    SoTienTrenMotDiemTich = table.Column<decimal>(type: "numeric", nullable: false),
                    SoTienGiamTrenMotDiem = table.Column<decimal>(type: "numeric", nullable: false),
                    TinhApDungPhiShip = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TrangThai = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CauHinhBanHangs", x => x.IDCauHinhBanHang);
                });

            migrationBuilder.CreateTable(
                name: "HangKhachHangs",
                columns: table => new
                {
                    IDHangKhachHang = table.Column<Guid>(type: "uuid", nullable: false),
                    MaHang = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TenHang = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DiemTu = table.Column<int>(type: "integer", nullable: false),
                    DiemDen = table.Column<int>(type: "integer", nullable: true),
                    PhanTramGiamPhiShip = table.Column<decimal>(type: "numeric", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LanCapNhatCuoi = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TrangThai = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HangKhachHangs", x => x.IDHangKhachHang);
                });

            migrationBuilder.CreateTable(
                name: "LichSuDiemKhachHangs",
                columns: table => new
                {
                    IDLichSuDiemKhachHang = table.Column<Guid>(type: "uuid", nullable: false),
                    IDKhachHang = table.Column<Guid>(type: "uuid", nullable: false),
                    IDHoaDon = table.Column<Guid>(type: "uuid", nullable: true),
                    LoaiBienDong = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SoDiemBienDong = table.Column<int>(type: "integer", nullable: false),
                    SoDiemTruoc = table.Column<int>(type: "integer", nullable: false),
                    SoDiemSau = table.Column<int>(type: "integer", nullable: false),
                    MoTa = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TrangThai = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichSuDiemKhachHangs", x => x.IDLichSuDiemKhachHang);
                    table.ForeignKey(
                        name: "FK_LichSuDiemKhachHangs_HoaDons_IDHoaDon",
                        column: x => x.IDHoaDon,
                        principalTable: "HoaDons",
                        principalColumn: "IDHoaDon",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LichSuDiemKhachHangs_KhachHang_IDKhachHang",
                        column: x => x.IDKhachHang,
                        principalTable: "KhachHang",
                        principalColumn: "IDKhachHang",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "CauHinhBanHangs",
                columns: new[] { "IDCauHinhBanHang", "DanhSachQuanHuyenNoiThanh", "DiemToiDaSuDungMoiDon", "LanCapNhatCuoi", "NgayTao", "NguoiCapNhat", "NguoiTao", "NguonTinhPhiShipMacDinh", "PhiShipMacDinh", "PhiShipNgoaiThanh", "PhiShipNoiThanh", "PhiShipToanQuoc", "SoTienGiamTrenMotDiem", "SoTienTrenMotDiemTich", "TinhApDungPhiShip", "TrangThai" },
                values: new object[] { new Guid("8f3aa6f2-0608-4f47-a406-5de9ef3366d2"), "Ba Đình,Hoàn Kiếm,Tây Hồ,Long Biên,Cầu Giấy,Đống Đa,Hai Bà Trưng,Hoàng Mai,Thanh Xuân,Nam Từ Liêm,Bắc Từ Liêm,Hà Đông", 0, null, new DateTime(2026, 3, 28, 2, 19, 6, 217, DateTimeKind.Utc).AddTicks(7425), null, "System", "GHN", 50000m, 35000m, 20000m, 50000m, 1000m, 10000m, "Hà Nội", true });

            migrationBuilder.InsertData(
                table: "HangKhachHangs",
                columns: new[] { "IDHangKhachHang", "DiemDen", "DiemTu", "LanCapNhatCuoi", "MaHang", "NgayTao", "NguoiCapNhat", "NguoiTao", "PhanTramGiamPhiShip", "TenHang", "TrangThai" },
                values: new object[,]
                {
                    { new Guid("38fbce12-fc6f-4d1f-badf-fc2b70f6c396"), null, 3000, null, "PLATINUM", new DateTime(2026, 3, 28, 2, 19, 6, 217, DateTimeKind.Utc).AddTicks(7461), null, "System", 30m, "Bạch kim", true },
                    { new Guid("49cb8a18-d15d-4df5-97be-f98f6ef88ca4"), 1499, 500, null, "SILVER", new DateTime(2026, 3, 28, 2, 19, 6, 217, DateTimeKind.Utc).AddTicks(7455), null, "System", 10m, "Bạc", true },
                    { new Guid("b72f6741-bfd9-4c29-8aeb-6f4f2b8cbf3d"), 2999, 1500, null, "GOLD", new DateTime(2026, 3, 28, 2, 19, 6, 217, DateTimeKind.Utc).AddTicks(7458), null, "System", 20m, "Vàng", true },
                    { new Guid("d58c8c83-0f0a-48a8-a2dd-c7ac8f668f69"), 499, 0, null, "BRONZE", new DateTime(2026, 3, 28, 2, 19, 6, 217, DateTimeKind.Utc).AddTicks(7452), null, "System", 0m, "Đồng", true }
                });

            migrationBuilder.UpdateData(
                table: "VaiTro",
                keyColumn: "IDVaiTro",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d"),
                column: "NgayTao",
                value: new DateTime(2026, 3, 28, 2, 19, 6, 217, DateTimeKind.Utc).AddTicks(7228));

            migrationBuilder.UpdateData(
                table: "VaiTro",
                keyColumn: "IDVaiTro",
                keyValue: new Guid("b2c3d4e5-f6a7-5b6c-9d0e-1f2a3b4c5d6e"),
                column: "NgayTao",
                value: new DateTime(2026, 3, 28, 2, 19, 6, 217, DateTimeKind.Utc).AddTicks(7231));

            migrationBuilder.UpdateData(
                table: "VaiTro",
                keyColumn: "IDVaiTro",
                keyValue: new Guid("c3d4e5f6-a7b8-6c7d-0e1f-2a3b4c5d6e7f"),
                column: "NgayTao",
                value: new DateTime(2026, 3, 28, 2, 19, 6, 217, DateTimeKind.Utc).AddTicks(7233));

            migrationBuilder.CreateIndex(
                name: "IX_KhachHang_IDHangKhachHang",
                table: "KhachHang",
                column: "IDHangKhachHang");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuDiemKhachHangs_IDHoaDon",
                table: "LichSuDiemKhachHangs",
                column: "IDHoaDon");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuDiemKhachHangs_IDKhachHang",
                table: "LichSuDiemKhachHangs",
                column: "IDKhachHang");

            migrationBuilder.AddForeignKey(
                name: "FK_KhachHang_HangKhachHangs_IDHangKhachHang",
                table: "KhachHang",
                column: "IDHangKhachHang",
                principalTable: "HangKhachHangs",
                principalColumn: "IDHangKhachHang",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KhachHang_HangKhachHangs_IDHangKhachHang",
                table: "KhachHang");

            migrationBuilder.DropTable(
                name: "CauHinhBanHangs");

            migrationBuilder.DropTable(
                name: "HangKhachHangs");

            migrationBuilder.DropTable(
                name: "LichSuDiemKhachHangs");

            migrationBuilder.DropIndex(
                name: "IX_KhachHang_IDHangKhachHang",
                table: "KhachHang");

            migrationBuilder.DropColumn(
                name: "IDHangKhachHang",
                table: "KhachHang");

            migrationBuilder.DropColumn(
                name: "SoDiemHienTai",
                table: "KhachHang");

            migrationBuilder.DropColumn(
                name: "TongDiemTichLuy",
                table: "KhachHang");

            migrationBuilder.DropColumn(
                name: "DiemCong",
                table: "HoaDons");

            migrationBuilder.DropColumn(
                name: "DiemDaDung",
                table: "HoaDons");

            migrationBuilder.DropColumn(
                name: "PhiVanChuyenGoc",
                table: "HoaDons");

            migrationBuilder.DropColumn(
                name: "SoTienGiamPhiVanChuyen",
                table: "HoaDons");

            migrationBuilder.DropColumn(
                name: "SoTienGiamTuDiem",
                table: "HoaDons");

            migrationBuilder.UpdateData(
                table: "VaiTro",
                keyColumn: "IDVaiTro",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d"),
                column: "NgayTao",
                value: new DateTime(2026, 3, 18, 10, 10, 20, 10, DateTimeKind.Utc).AddTicks(4224));

            migrationBuilder.UpdateData(
                table: "VaiTro",
                keyColumn: "IDVaiTro",
                keyValue: new Guid("b2c3d4e5-f6a7-5b6c-9d0e-1f2a3b4c5d6e"),
                column: "NgayTao",
                value: new DateTime(2026, 3, 18, 10, 10, 20, 10, DateTimeKind.Utc).AddTicks(4229));

            migrationBuilder.UpdateData(
                table: "VaiTro",
                keyColumn: "IDVaiTro",
                keyValue: new Guid("c3d4e5f6-a7b8-6c7d-0e1f-2a3b4c5d6e7f"),
                column: "NgayTao",
                value: new DateTime(2026, 3, 18, 10, 10, 20, 10, DateTimeKind.Utc).AddTicks(4232));
        }
    }
}
