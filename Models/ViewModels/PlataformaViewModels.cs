using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace StockSuper.Models.ViewModels
{
    // Alta de una empresa cliente junto con su casa central. Una empresa no puede
    // existir sin su sucursal principal, por eso se cargan en el mismo formulario.
    public class EmpresaCrearViewModel
    {
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

        // Datos de la casa central
        [Required(ErrorMessage = "El código de la casa central es obligatorio.")]
        [StringLength(10, ErrorMessage = "El código no puede superar los 10 caracteres.")]
        [Display(Name = "Código de sucursal")]
        public string CentralCodigo { get; set; } = "001";

        [Required(ErrorMessage = "El nombre de la casa central es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
        [Display(Name = "Nombre")]
        public string CentralNombre { get; set; } = "Casa Central";

        [Required(ErrorMessage = "La dirección es obligatoria.")]
        [StringLength(200, ErrorMessage = "La dirección no puede superar los 200 caracteres.")]
        [Display(Name = "Dirección")]
        public string CentralDireccion { get; set; } = string.Empty;

        [Required(ErrorMessage = "La localidad es obligatoria.")]
        [StringLength(100, ErrorMessage = "La localidad no puede superar los 100 caracteres.")]
        [Display(Name = "Localidad")]
        public string CentralLocalidad { get; set; } = string.Empty;

        [Required(ErrorMessage = "La provincia es obligatoria.")]
        [StringLength(100, ErrorMessage = "La provincia no puede superar los 100 caracteres.")]
        [Display(Name = "Provincia")]
        public string CentralProvincia { get; set; } = string.Empty;

    }

    // Alta y edición de una sucursal común. La casa central se crea junto con la empresa.
    public class SucursalFormViewModel
    {
        public int Id { get; set; }

        // Se elige solo al crear; en la edición la empresa no se puede cambiar.
        // Se valida a mano en el controlador (no lleva [Required] porque en la edición no se envía).
        [Display(Name = "Empresa")]
        public int? EmpresaId { get; set; }

        [Required(ErrorMessage = "El código de sucursal es obligatorio.")]
        [StringLength(10, ErrorMessage = "El código no puede superar los 10 caracteres.")]
        [Display(Name = "Código de sucursal")]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre de la sucursal es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La dirección es obligatoria.")]
        [StringLength(200, ErrorMessage = "La dirección no puede superar los 200 caracteres.")]
        [Display(Name = "Dirección")]
        public string Direccion { get; set; } = string.Empty;

        [Required(ErrorMessage = "La localidad es obligatoria.")]
        [StringLength(100, ErrorMessage = "La localidad no puede superar los 100 caracteres.")]
        [Display(Name = "Localidad")]
        public string Localidad { get; set; } = string.Empty;

        [Required(ErrorMessage = "La provincia es obligatoria.")]
        [StringLength(100, ErrorMessage = "La provincia no puede superar los 100 caracteres.")]
        [Display(Name = "Provincia")]
        public string Provincia { get; set; } = string.Empty;


        // Solo lectura en la edición
        public string EmpresaNombre { get; set; } = string.Empty;
        public bool EsCasaCentral { get; set; }

        public IEnumerable<SelectListItem> Empresas { get; set; } = new List<SelectListItem>();
    }
}