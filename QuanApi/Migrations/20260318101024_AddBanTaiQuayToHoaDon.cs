using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanApi.Migrations
{
    /// <inheritdoc />
    public partial class AddBanTaiQuayToHoaDon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BanTaiQuay",
                table: "HoaDons",
                type: "boolean",
                nullable: false,
                defaultValue: false);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BanTaiQuay",
                table: "HoaDons");

            migrationBuilder.UpdateData(
                table: "VaiTro",
                keyColumn: "IDVaiTro",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d"),
                column: "NgayTao",
                value: new DateTime(2026, 1, 30, 18, 50, 46, 167, DateTimeKind.Utc).AddTicks(5897));

            migrationBuilder.UpdateData(
                table: "VaiTro",
                keyColumn: "IDVaiTro",
                keyValue: new Guid("b2c3d4e5-f6a7-5b6c-9d0e-1f2a3b4c5d6e"),
                column: "NgayTao",
                value: new DateTime(2026, 1, 30, 18, 50, 46, 167, DateTimeKind.Utc).AddTicks(5900));

            migrationBuilder.UpdateData(
                table: "VaiTro",
                keyColumn: "IDVaiTro",
                keyValue: new Guid("c3d4e5f6-a7b8-6c7d-0e1f-2a3b4c5d6e7f"),
                column: "NgayTao",
                value: new DateTime(2026, 1, 30, 18, 50, 46, 167, DateTimeKind.Utc).AddTicks(5903));
        }
    }
}
