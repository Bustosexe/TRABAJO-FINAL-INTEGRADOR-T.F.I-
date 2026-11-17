using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace StockSuper.Models.ViewModels
{
    // Tipos de cuenta que puede crear el administrador de la plataforma.
    // El tipo define el rol y el alcance: toda la empresa o una sola sucursal.
    public static class TiposCuenta
    {
        public const string Direccion = "direccion";
        public const string Sucursal = "sucursal";
    }

    // Sucursal ofrecida en el formulario, con su empresa para filtrar la lista en el navegador
    public class SucursalOpcion
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public string Texto { get; set; } = string.Empty;
    }

    // Alta de una cuenta de acceso. La contraseña no se carga: el sistema genera una
    // temporal y obliga a cambiarla en el primer ingreso.
    public class CuentaCrearViewModel
    {
        [Required(ErrorMessage = "Seleccione una empresa.")]
        [Display(Name = "Empresa")]
        public int? EmpresaId { get; set; }

        [Required(ErrorMessage = "Seleccione el tipo de cuenta.")]
        [Display(Name = "Tipo de cuenta")]
        public string Tipo { get; set; } = TiposCuenta.Direccion;

        // Obligatoria solo para cuentas de sucursal (se valida en el controlador)
        [Display(Name = "Sucursal")]
        public int? SucursalId { get; set; }

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
        [StringLength(100, ErrorMessage = "El correo no puede superar los 100 caracteres.")]
        [Display(Name = "Correo electrónico (usuario de ingreso)")]
        public string Email { get; set; } = string.Empty;

        // Opcional: si queda vacío se arma con la empresa y la sucursal
        [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
        [Display(Name = "Nombre visible de la cuenta")]
        public string? NombreCuenta { get; set; }

        public IEnumerable<SelectListItem> Empresas { get; set; } = new List<SelectListItem>();
        public List<SucursalOpcion> Sucursales { get; set; } = new();
    }

    // Edición de una cuenta: solo el correo y el nombre visible. La empresa, la sucursal
    // y el rol quedan fijos desde el alta.
    public class CuentaEditarViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
        [StringLength(100, ErrorMessage = "El correo no puede superar los 100 caracteres.")]
        [Display(Name = "Correo electrónico (usuario de ingreso)")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre de la cuenta es obligatorio.")]
        [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
        [Display(Name = "Nombre visible de la cuenta")]
        public string NombreCuenta { get; set; } = string.Empty;

        // Solo lectura
        public string EmpresaNombre { get; set; } = string.Empty;
        public string Alcance { get; set; } = string.Empty;
    }

    // Fila del listado de cuentas
    public class CuentaListadoViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string NombreCuenta { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Empresa { get; set; } = string.Empty;
        public bool EmpresaActiva { get; set; }
        public string? Sucursal { get; set; }
        public bool SucursalCerrada { get; set; }
        public bool Activo { get; set; }
        public bool DebeCambiarPassword { get; set; }
    }

    // Credenciales recién generadas. Se muestran una sola vez para entregarlas al cliente.
    public class CredencialesViewModel
    {
        public string UsuarioId { get; set; } = string.Empty;
        public string NombreCuenta { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordTemporal { get; set; } = string.Empty;
        public string UrlIngreso { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public string EnlaceWhatsApp { get; set; } = string.Empty;
        public bool EsRestablecimiento { get; set; }
        public bool CorreoConfigurado { get; set; }
    }
}
