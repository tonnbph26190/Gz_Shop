using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanApi.Migrations
{
    /// <inheritdoc />
    public partial class update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BannerSanPhams",
                columns: table => new
                {
                    IDBannerSanPham = table.Column<Guid>(type: "uuid", nullable: false),
                    BannerId = table.Column<int>(type: "integer", nullable: false),
                    IDSanPham = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BannerSanPhams", x => x.IDBannerSanPham);
                    table.ForeignKey(
                        name: "FK_BannerSanPhams_Banners_BannerId",
                        column: x => x.BannerId,
                        principalTable: "Banners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BannerSanPhams_SanPhams_IDSanPham",
                        column: x => x.IDSanPham,
                        principalTable: "SanPhams",
                        principalColumn: "IDSanPham",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "CauHinhBanHangs",
                keyColumn: "IDCauHinhBanHang",
                keyValue: new Guid("8f3aa6f2-0608-4f47-a406-5de9ef3366d2"),
                columns: new[] { "NgayTao", "NguonTinhPhiShipMacDinh" },
                values: new object[] { new DateTime(2026, 3, 28, 5, 48, 31, 464, DateTimeKind.Utc).AddTicks(757), "Config" });

            migrationBuilder.UpdateData(
                table: "HangKhachHangs",
                keyColumn: "IDHangKhachHang",
                keyValue: new Guid("38fbce12-fc6f-4d1f-badf-fc2b70f6c396"),
                column: "NgayTao",
                value: new DateTime(2026, 3, 28, 5, 48, 31, 464, DateTimeKind.Utc).AddTicks(787));

            migrationBuilder.UpdateData(
                table: "HangKhachHangs",
                keyColumn: "IDHangKhachHang",
                keyValue: new Guid("49cb8a18-d15d-4df5-97be-f98f6ef88ca4"),
                column: "NgayTao",
                value: new DateTime(2026, 3, 28, 5, 48, 31, 464, DateTimeKind.Utc).AddTicks(781));

            migrationBuilder.UpdateData(
                table: "HangKhachHangs",
                keyColumn: "IDHangKhachHang",
                keyValue: new Guid("b72f6741-bfd9-4c29-8aeb-6f4f2b8cbf3d"),
                column: "NgayTao",
                value: new DateTime(2026, 3, 28, 5, 48, 31, 464, DateTimeKind.Utc).AddTicks(784));

            migrationBuilder.UpdateData(
                table: "HangKhachHangs",
                keyColumn: "IDHangKhachHang",
                keyValue: new Guid("d58c8c83-0f0a-48a8-a2dd-c7ac8f668f69"),
                column: "NgayTao",
                value: new DateTime(2026, 3, 28, 5, 48, 31, 464, DateTimeKind.Utc).AddTicks(778));

            migrationBuilder.UpdateData(
                table: "VaiTro",
                keyColumn: "IDVaiTro",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d"),
                column: "NgayTao",
                value: new DateTime(2026, 3, 28, 5, 48, 31, 464, DateTimeKind.Utc).AddTicks(648));

            migrationBuilder.UpdateData(
                table: "VaiTro",
                keyColumn: "IDVaiTro",
                keyValue: new Guid("b2c3d4e5-f6a7-5b6c-9d0e-1f2a3b4c5d6e"),
                column: "NgayTao",
                value: new DateTime(2026, 3, 28, 5, 48, 31, 464, DateTimeKind.Utc).AddTicks(651));

            migrationBuilder.UpdateData(
                table: "VaiTro",
                keyColumn: "IDVaiTro",
                keyValue: new Guid("c3d4e5f6-a7b8-6c7d-0e1f-2a3b4c5d6e7f"),
                column: "NgayTao",
                value: new DateTime(2026, 3, 28, 5, 48, 31, 464, DateTimeKind.Utc).AddTicks(653));

            migrationBuilder.CreateIndex(
                name: "IX_BannerSanPhams_BannerId_IDSanPham",
                table: "BannerSanPhams",
                columns: new[] { "BannerId", "IDSanPham" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BannerSanPhams_IDSanPham",
                table: "BannerSanPhams",
                column: "IDSanPham");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BannerSanPhams");

            migrationBuilder.UpdateData(
                table: "CauHinhBanHangs",
                keyColumn: "IDCauHinhBanHang",
                keyValue: new Guid("8f3aa6f2-0608-4f47-a406-5de9ef3366d2"),
                columns: new[] { "NgayTao", "NguonTinhPhiShipMacDinh" },
                values: new object[] { new DateTime(2026, 3, 28, 2, 19, 6, 217, DateTimeKind.Utc).AddTicks(7425), "GHN" });

            migrationBuilder.UpdateData(
                table: "HangKhachHangs",
                keyColumn: "IDHangKhachHang",
                keyValue: new Guid("38fbce12-fc6f-4d1f-badf-fc2b70f6c396"),
                column: "NgayTao",
                value: new DateTime(2026, 3, 28, 2, 19, 6, 217, DateTimeKind.Utc).AddTicks(7461));

            migrationBuilder.UpdateData(
                table: "HangKhachHangs",
                keyColumn: "IDHangKhachHang",
                keyValue: new Guid("49cb8a18-d15d-4df5-97be-f98f6ef88ca4"),
                column: "NgayTao",
                value: new DateTime(2026, 3, 28, 2, 19, 6, 217, DateTimeKind.Utc).AddTicks(7455));

            migrationBuilder.UpdateData(
                table: "HangKhachHangs",
                keyColumn: "IDHangKhachHang",
                keyValue: new Guid("b72f6741-bfd9-4c29-8aeb-6f4f2b8cbf3d"),
                column: "NgayTao",
                value: new DateTime(2026, 3, 28, 2, 19, 6, 217, DateTimeKind.Utc).AddTicks(7458));

            migrationBuilder.UpdateData(
                table: "HangKhachHangs",
                keyColumn: "IDHangKhachHang",
                keyValue: new Guid("d58c8c83-0f0a-48a8-a2dd-c7ac8f668f69"),
                column: "NgayTao",
                value: new DateTime(2026, 3, 28, 2, 19, 6, 217, DateTimeKind.Utc).AddTicks(7452));

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
        }
    }
}
