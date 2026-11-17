using System.ComponentModel.DataAnnotations;

namespace StockSuper.Models
{
    // Cliente del sistema: una empresa con contrato vigente (ej.: La Anónima, Carrefour).
    // Es la raíz del aislamiento de datos. No hereda EmpresaEntity porque ella misma es el tenant.
    public class Empresa : AuditableEntity
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "La razón social es obligatoria.")]
        [StringLength(150, ErrorMessage = "La razón social no puede superar los 150 caracteres.")]
        [Display(Name = "Razón social")]
        public string RazonSocial { get; set; } = string.Empty;

        [Required(ErrorMessage = "El CUIT es obligatorio.")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "El CUIT debe tener 11 dígitos, sin guiones.")]
        [Display(Name = "CUIT")]
        public string Cuit { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
        [StringLength(100, ErrorMessage = "El correo no puede superar los 100 caracteres.")]
        [Display(Name = "Correo de contacto")]
        public string? Email { get; set; }

        [StringLength(20, ErrorMessage = "El teléfono no puede superar los 20 caracteres.")]
        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Inicio de contrato")]
        public DateTime? FechaContrato { get; set; }

        // Una empresa suspendida (ej.: contrato vencido) no puede iniciar sesión
        [Display(Name = "Activa")]
        public bool Activa { get; set; } = true;

        public ICollection<Sucursal> Sucursales { get; set; } = new List<Sucursal>();
    }
}