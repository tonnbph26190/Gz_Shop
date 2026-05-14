using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanApi.Migrations
{
    /// <inheritdoc />
    public partial class AddTrangThaiThanhToanToHoaDon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TrangThaiThanhToan",
                table: "HoaDons",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Chưa thanh toán");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrangThaiThanhToan",
                table: "HoaDons");
        }
    }
}
