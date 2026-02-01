using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace QuanApi.Migrations
{
    /// <inheritdoc />
    public partial class SeedVaiTroData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "VaiTro",
                columns: new[] { "IDVaiTro", "LanCapNhatCuoi", "MaVaiTro", "NgayTao", "NguoiCapNhat", "NguoiTao", "TenVaiTro", "TrangThai" },
                values: new object[,]
                {
                    { new Guid("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d"), null, "ADMIN", new DateTime(2026, 1, 30, 18, 50, 46, 167, DateTimeKind.Utc).AddTicks(5897), null, "System", "Quản trị viên", true },
                    { new Guid("b2c3d4e5-f6a7-5b6c-9d0e-1f2a3b4c5d6e"), null, "NHANVIEN", new DateTime(2026, 1, 30, 18, 50, 46, 167, DateTimeKind.Utc).AddTicks(5900), null, "System", "Nhân viên", true },
                    { new Guid("c3d4e5f6-a7b8-6c7d-0e1f-2a3b4c5d6e7f"), null, "KHACHHANG", new DateTime(2026, 1, 30, 18, 50, 46, 167, DateTimeKind.Utc).AddTicks(5903), null, "System", "Khách hàng", true }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "VaiTro",
                keyColumn: "IDVaiTro",
                keyValue: new Guid("a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d"));

            migrationBuilder.DeleteData(
                table: "VaiTro",
                keyColumn: "IDVaiTro",
                keyValue: new Guid("b2c3d4e5-f6a7-5b6c-9d0e-1f2a3b4c5d6e"));

            migrationBuilder.DeleteData(
                table: "VaiTro",
                keyColumn: "IDVaiTro",
                keyValue: new Guid("c3d4e5f6-a7b8-6c7d-0e1f-2a3b4c5d6e7f"));
        }
    }
}
