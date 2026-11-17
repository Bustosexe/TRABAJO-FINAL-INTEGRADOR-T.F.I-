using System.ComponentModel.DataAnnotations;

namespace StockSuper.Models
{
    // Empresa que abastece mercadería. Cada cliente maneja su propia cartera
    // de proveedores. La baja es lógica (Activo = false).
    public class Proveedor : EmpresaEntity
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

        [StringLength(20, ErrorMessage = "El teléfono no puede superar los 20 caracteres.")]
        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        // Destino de los correos de reposición generados con IA
        [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
        [StringLength(100, ErrorMessage = "El correo no puede superar los 100 caracteres.")]
        [Display(Name = "Correo electrónico")]
        public string? Email { get; set; }

        [StringLength(200, ErrorMessage = "La dirección no puede superar los 200 caracteres.")]
        [Display(Name = "Dirección")]
        public string? Direccion { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;

        public ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }
}