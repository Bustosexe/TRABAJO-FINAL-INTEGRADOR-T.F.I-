using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSuper.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmpresaId = table.Column<int>(type: "int", nullable: true),
                    SucursalId = table.Column<int>(type: "int", nullable: true),
                    NombreCuenta = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DebeCambiarPassword = table.Column<bool>(type: "bit", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Empresas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RazonSocial = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Cuit = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FechaContrato = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    UsuarioCreacionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioModificacionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Empresas_AspNetUsers_UsuarioCreacionId",
                        column: x => x.UsuarioCreacionId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Empresas_AspNetUsers_UsuarioModificacionId",
                        column: x => x.UsuarioModificacionId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Categorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ControlaVencimiento = table.Column<bool>(type: "bit", nullable: false),
                    UsuarioCreacionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioModificacionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmpresaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categorias_AspNetUsers_UsuarioCreacionId",
                        column: x => x.UsuarioCreacionId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Categorias_AspNetUsers_UsuarioModificacionId",
                        column: x => x.UsuarioModificacionId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Categorias_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Marcas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UsuarioCreacionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioModificacionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmpresaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Marcas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Marcas_AspNetUsers_UsuarioCreacionId",
                        column: x => x.UsuarioCreacionId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Marcas_AspNetUsers_UsuarioModificacionId",
                        column: x => x.UsuarioModificacionId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Marcas_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Proveedores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RazonSocial = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Cuit = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Direccion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    UsuarioCreacionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioModificacionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmpresaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proveedores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Proveedores_AspNetUsers_UsuarioCreacionId",
                        column: x => x.UsuarioCreacionId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Proveedores_AspNetUsers_UsuarioModificacionId",
                        column: x => x.UsuarioModificacionId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Proveedores_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sucursales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EsCasaCentral = table.Column<bool>(type: "bit", nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Localidad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Provincia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Latitud = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    Longitud = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    UsuarioCreacionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioModificacionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmpresaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sucursales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sucursales_AspNetUsers_UsuarioCreacionId",
                        column: x => x.UsuarioCreacionId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sucursales_AspNetUsers_UsuarioModificacionId",
                        column: x => x.UsuarioModificacionId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sucursales_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Productos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoSku = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PrecioVenta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StockMinimo = table.Column<int>(type: "int", nullable: false),
                    ImagenRuta = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    CategoriaId = table.Column<int>(type: "int", nullable: false),
                    MarcaId = table.Column<int>(type: "int", nullable: false),
                    ProveedorId = table.Column<int>(type: "int", nullable: false),
                    UsuarioCreacionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioModificacionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmpresaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Productos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Productos_AspNetUsers_UsuarioCreacionId",
                        column: x => x.UsuarioCreacionId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Productos_AspNetUsers_UsuarioModificacionId",
                        column: x => x.UsuarioModificacionId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Productos_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Productos_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Productos_Marcas_MarcaId",
                        column: x => x.MarcaId,
                        principalTable: "Marcas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Productos_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SectoresDeposito",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreSector = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Pasillo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Estante = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    UsuarioCreacionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioModificacionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    SucursalId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectoresDeposito", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SectoresDeposito_AspNetUsers_UsuarioCreacionId",
                        column: x => x.UsuarioCreacionId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SectoresDeposito_AspNetUsers_UsuarioModificacionId",
                        column: x => x.UsuarioModificacionId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SectoresDeposito_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SectoresDeposito_Sucursales_SucursalId",
                        column: x => x.SucursalId,
                        principalTable: "Sucursales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Pallets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoPallet = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CodigoLote = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    SectorDepositoId = table.Column<int>(type: "int", nullable: false),
                    CantidadInicial = table.Column<int>(type: "int", nullable: false),
                    CantidadActual = table.Column<int>(type: "int", nullable: false),
                    PrecioCompra = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FechaElaboracion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioCreacionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioModificacionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    SucursalId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pallets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pallets_AspNetUsers_UsuarioCreacionId",
                        column: x => x.UsuarioCreacionId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pallets_AspNetUsers_UsuarioModificacionId",
                        column: x => x.UsuarioModificacionId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pallets_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pallets_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pallets_SectoresDeposito_SectorDepositoId",
                        column: x => x.SectorDepositoId,
                        principalTable: "SectoresDeposito",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pallets_Sucursales_SucursalId",
                        column: x => x.SucursalId,
                        principalTable: "Sucursales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MovimientosStock",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PalletId = table.Column<int>(type: "int", nullable: false),
                    FechaMovimiento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TipoMovimiento = table.Column<int>(type: "int", nullable: false),
                    Motivo = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    Observacion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UsuarioCreacionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioModificacionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    SucursalId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosStock", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimientosStock_AspNetUsers_UsuarioCreacionId",
                        column: x => x.UsuarioCreacionId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientosStock_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientosStock_AspNetUsers_UsuarioModificacionId",
                        column: x => x.UsuarioModificacionId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientosStock_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientosStock_Pallets_PalletId",
                        column: x => x.PalletId,
                        principalTable: "Pallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientosStock_Sucursales_SucursalId",
                        column: x => x.SucursalId,
                        principalTable: "Sucursales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_EmpresaId",
                table: "AspNetUsers",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_SucursalId",
                table: "AspNetUsers",
                column: "SucursalId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_EmpresaId_Nombre",
                table: "Categorias",
                columns: new[] { "EmpresaId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_UsuarioCreacionId",
                table: "Categorias",
                column: "UsuarioCreacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_UsuarioModificacionId",
                table: "Categorias",
                column: "UsuarioModificacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Empresas_Cuit",
                table: "Empresas",
                column: "Cuit",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Empresas_UsuarioCreacionId",
                table: "Empresas",
                column: "UsuarioCreacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Empresas_UsuarioModificacionId",
                table: "Empresas",
                column: "UsuarioModificacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Marcas_EmpresaId_Nombre",
                table: "Marcas",
                columns: new[] { "EmpresaId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Marcas_UsuarioCreacionId",
                table: "Marcas",
                column: "UsuarioCreacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Marcas_UsuarioModificacionId",
                table: "Marcas",
                column: "UsuarioModificacionId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_EmpresaId",
                table: "MovimientosStock",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_PalletId",
                table: "MovimientosStock",
                column: "PalletId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_SucursalId",
                table: "MovimientosStock",
                column: "SucursalId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_UsuarioCreacionId",
                table: "MovimientosStock",
                column: "UsuarioCreacionId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_UsuarioId",
                table: "MovimientosStock",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_UsuarioModificacionId",
                table: "MovimientosStock",
                column: "UsuarioModificacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Pallets_EmpresaId",
                table: "Pallets",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Pallets_ProductoId",
                table: "Pallets",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_Pallets_SectorDepositoId",
                table: "Pallets",
                column: "SectorDepositoId");

            migrationBuilder.CreateIndex(
                name: "IX_Pallets_SucursalId_CodigoPallet",
                table: "Pallets",
                columns: new[] { "SucursalId", "CodigoPallet" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pallets_UsuarioCreacionId",
                table: "Pallets",
                column: "UsuarioCreacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Pallets_UsuarioModificacionId",
                table: "Pallets",
                column: "UsuarioModificacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_CategoriaId",
                table: "Productos",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_EmpresaId_CodigoSku",
                table: "Productos",
                columns: new[] { "EmpresaId", "CodigoSku" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Productos_MarcaId",
                table: "Productos",
                column: "MarcaId");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_ProveedorId",
                table: "Productos",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_UsuarioCreacionId",
                table: "Productos",
                column: "UsuarioCreacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_UsuarioModificacionId",
                table: "Productos",
                column: "UsuarioModificacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_EmpresaId_Cuit",
                table: "Proveedores",
                columns: new[] { "EmpresaId", "Cuit" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_UsuarioCreacionId",
                table: "Proveedores",
                column: "UsuarioCreacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_UsuarioModificacionId",
                table: "Proveedores",
                column: "UsuarioModificacionId");

            migrationBuilder.CreateIndex(
                name: "IX_SectoresDeposito_EmpresaId",
                table: "SectoresDeposito",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_SectoresDeposito_SucursalId",
                table: "SectoresDeposito",
                column: "SucursalId");

            migrationBuilder.CreateIndex(
                name: "IX_SectoresDeposito_UsuarioCreacionId",
                table: "SectoresDeposito",
                column: "UsuarioCreacionId");

            migrationBuilder.CreateIndex(
                name: "IX_SectoresDeposito_UsuarioModificacionId",
                table: "SectoresDeposito",
                column: "UsuarioModificacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Sucursales_EmpresaId_Codigo",
                table: "Sucursales",
                columns: new[] { "EmpresaId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sucursales_UsuarioCreacionId",
                table: "Sucursales",
                column: "UsuarioCreacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Sucursales_UsuarioModificacionId",
                table: "Sucursales",
                column: "UsuarioModificacionId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Empresas_EmpresaId",
                table: "AspNetUsers",
                column: "EmpresaId",
                principalTable: "Empresas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Sucursales_SucursalId",
                table: "AspNetUsers",
                column: "SucursalId",
                principalTable: "Sucursales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Empresas_AspNetUsers_UsuarioCreacionId",
                table: "Empresas");

            migrationBuilder.DropForeignKey(
                name: "FK_Empresas_AspNetUsers_UsuarioModificacionId",
                table: "Empresas");

            migrationBuilder.DropForeignKey(
                name: "FK_Sucursales_AspNetUsers_UsuarioCreacionId",
                table: "Sucursales");

            migrationBuilder.DropForeignKey(
                name: "FK_Sucursales_AspNetUsers_UsuarioModificacionId",
                table: "Sucursales");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "MovimientosStock");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Pallets");

            migrationBuilder.DropTable(
                name: "Productos");

            migrationBuilder.DropTable(
                name: "SectoresDeposito");

            migrationBuilder.DropTable(
                name: "Categorias");

            migrationBuilder.DropTable(
                name: "Marcas");

            migrationBuilder.DropTable(
                name: "Proveedores");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Sucursales");

            migrationBuilder.DropTable(
                name: "Empresas");
        }
    }
}
