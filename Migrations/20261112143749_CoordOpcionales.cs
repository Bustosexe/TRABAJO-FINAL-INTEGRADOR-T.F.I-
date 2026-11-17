using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSuper.Migrations
{
    /// <inheritdoc />
    public partial class CoordOpcionales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Longitud",
                table: "Sucursales",
                type: "decimal(9,6)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Latitud",
                table: "Sucursales",
                type: "decimal(9,6)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Longitud",
                table: "Sucursales",
                type: "decimal(9,6)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Latitud",
                table: "Sucursales",
                type: "decimal(9,6)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)",
                oldNullable: true);
        }
    }
}
