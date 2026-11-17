using Microsoft.AspNetCore.Identity;

namespace StockSuper.Models
{
    // Extiende IdentityUser (AspNetUsers) con el vínculo a empresa y sucursal.
    // - Administrador de plataforma: EmpresaId y SucursalId nulos.
    // - Director de empresa: EmpresaId asignado, SucursalId nulo (todas las sucursales).
    // - Usuario de sucursal: EmpresaId y SucursalId asignados y fijos.
    public class ApplicationUser : IdentityUser
    {
        public int? EmpresaId { get; set; }
        public int? SucursalId { get; set; }

        // Nombre visible de la cuenta (ej.: "La Anónima - Sucursal 202")
        public string NombreCuenta { get; set; } = string.Empty;

        // Fuerza el cambio de la contraseña temporal en el primer ingreso
        public bool DebeCambiarPassword { get; set; } = true;

        public bool Activo { get; set; } = true;

        public Empresa? Empresa { get; set; }
        public Sucursal? Sucursal { get; set; }
    }
}