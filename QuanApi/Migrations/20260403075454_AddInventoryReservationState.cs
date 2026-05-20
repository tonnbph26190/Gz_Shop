using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanApi.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryReservationState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SoLuongDatCho",
                table: "SanPhamChiTiets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "DaDatChoTonKho",
                table: "HoaDons",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DaTruTonKho",
                table: "HoaDons",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SoLuongDatCho",
                table: "ChiTietGioHangs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "CauHinhBanHangs",
                keyColumn: "IDCauHinhBanHang",
                keyValue: new Guid("8f3aa6f2-0608-4f47-a406-5de9ef3366d2"),
                column: "NgayTao",
                value: new DateTime(2026, 4, 3, 7, 54, 53, 549, DateTimeKind.Utc).AddTicks(5852));

            migrationBuilder.UpdateData(
                table: "HangKhachHangs",
                keyColumn: "IDHangKhachHang",
                keyValue: new Guid("38fbce12-fc6f-4d1f-badf-fc2b70f6c396"),
                column: "NgayTao",
                value: new DateTime(2026, 4, 3, 7, 54, 53, 549, DateTimeKind.Utc).AddTicks(5887));

            migrationBuilder.UpdateData(
                table: "HangKhachHangs",
                keyColumn: "IDHangKhachHang",
                keyValue: new Guid("49cb8a18-d15d-4df5-97be-f98f6ef88ca4"),
                column: "NgayTao",
                value: new DateTime(2026, 4, 3, 7, 54, 53, 549, DateTimeKind.Utc).AddTicks(5879));

            migrationBuilder.UpdateData(
                table: "HangKhachHangs",
                keyColumn: "IDHangKhachHang",
                keyValue: new Guid("b72f6741-bfd9-4c29-8aeb-6f4f2b8cbf3d"),
                column: "NgayTao",
                value: new DateTime(2026, 4, 3, 7, 54, 53, 549, DateTimeKind.Utc).AddTicks(5881));

            migrationBuilder.UpdateData(
                table: "HangKhachHangs",
                keyColumn: "IDHangKhachHang",
                keyValue: new Guid("d58c8c83-0f0a-48a8-a2dd-c7ac8f668f69"),
                column: "NgayTao",
                value: new DateTime(2026, 4, 3, 7, 54, 53, 549, DateTimeKind.Utc).AddTicks(5875));

            migrationBuilder.UpdateData(
                table: "VaiTro",
                keyColumn: "IDVaiTro",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d"),
                column: "NgayTao",
                value: new DateTime(2026, 4, 3, 7, 54, 53, 549, DateTimeKind.Utc).AddTicks(5751));

            migrationBuilder.UpdateData(
                table: "VaiTro",
                keyColumn: "IDVaiTro",
                keyValue: new Guid("b2c3d4e5-f6a7-5b6c-9d0e-1f2a3b4c5d6e"),
                column: "NgayTao",
                value: new DateTime(2026, 4, 3, 7, 54, 53, 549, DateTimeKind.Utc).AddTicks(5754));

            migrationBuilder.UpdateData(
                table: "VaiTro",
                keyColumn: "IDVaiTro",
                keyValue: new Guid("c3d4e5f6-a7b8-6c7d-0e1f-2a3b4c5d6e7f"),
                column: "NgayTao",
                value: new DateTime(2026, 4, 3, 7, 54, 53, 549, DateTimeKind.Utc).AddTicks(5769));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SoLuongDatCho",
                table: "SanPhamChiTiets");

            migrationBuilder.DropColumn(
                name: "DaDatChoTonKho",
                table: "HoaDons");

            migrationBuilder.DropColumn(
                name: "DaTruTonKho",
                table: "HoaDons");

            migrationBuilder.DropColumn(
                name: "SoLuongDatCho",
                table: "ChiTietGioHangs");

            migrationBuilder.UpdateData(
                table: "CauHinhBanHangs",
                keyColumn: "IDCauHinhBanHang",
                keyValue: new Guid("8f3aa6f2-0608-4f47-a406-5de9ef3366d2"),
                column: "NgayTao",
                value: new DateTime(2026, 3, 28, 5, 48, 31, 464, DateTimeKind.Utc).AddTicks(757));

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
        }
    }
}
