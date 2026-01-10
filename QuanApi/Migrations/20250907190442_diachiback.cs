using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanApi.Migrations
{
    /// <inheritdoc />
    public partial class diachiback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhuongXa",
                table: "DiaChis");

            migrationBuilder.DropColumn(
                name: "QuanHuyen",
                table: "DiaChis");

            migrationBuilder.DropColumn(
                name: "TinhThanhPho",
                table: "DiaChis");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhuongXa",
                table: "DiaChis",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "QuanHuyen",
                table: "DiaChis",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TinhThanhPho",
                table: "DiaChis",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
