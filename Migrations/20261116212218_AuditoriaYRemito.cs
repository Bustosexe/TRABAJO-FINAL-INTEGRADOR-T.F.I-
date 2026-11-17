using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSuper.Migrations
{
    /// <inheritdoc />
    public partial class AuditoriaYRemito : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RemitoNombreOriginal",
                table: "Pallets",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RemitoRuta",
                table: "Pallets",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RegistrosAuditoria",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FechaUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Ambito = table.Column<int>(type: "int", nullable: false),
                    Accion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Entidad = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    EntidadId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Detalle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UsuarioNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    EmpresaId = table.Column<int>(type: "int", nullable: true),
                    SucursalId = table.Column<int>(type: "int", nullable: true),
                    Direccion = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosAuditoria", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_EmpresaId_Ambito_FechaUtc",
                table: "RegistrosAuditoria",
                columns: new[] { "EmpresaId", "Ambito", "FechaUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_FechaUtc",
                table: "RegistrosAuditoria",
                column: "FechaUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrosAuditoria");

            migrationBuilder.DropColumn(
                name: "RemitoNombreOriginal",
                table: "Pallets");

            migrationBuilder.DropColumn(
                name: "RemitoRuta",
                table: "Pallets");
        }
    }
}
