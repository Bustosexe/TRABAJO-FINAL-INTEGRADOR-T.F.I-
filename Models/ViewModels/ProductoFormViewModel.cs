using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace StockSuper.Models.ViewModels
{
    // Datos del formulario de producto. Se usa un ViewModel (y no la entidad) porque
    // el formulario necesita listas desplegables y un archivo adjunto, y porque
    // así el navegador nunca puede enviar empresa, auditoría ni estado.
    public class ProductoFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El código SKU es obligatorio.")]
        [StringLength(50, ErrorMessage = "El SKU no puede superar los 50 caracteres.")]
        [Display(Name = "Código SKU")]
        public string CodigoSku { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
        [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        // Nulable para que [Required] distinga "vacío" de "cero"
        [Required(ErrorMessage = "El precio de venta es obligatorio.")]
        [Range(0.01, 99999999, ErrorMessage = "El precio de venta debe ser mayor a cero.")]
        [Display(Name = "Precio de venta")]
        public decimal? PrecioVenta { get; set; }

        [Required(ErrorMessage = "El stock mínimo es obligatorio.")]
        [Range(0, 1000000, ErrorMessage = "El stock mínimo debe ser un número entero mayor o igual a cero.")]
        [Display(Name = "Stock mínimo")]
        public int? StockMinimo { get; set; }

        [Required(ErrorMessage = "Seleccione una categoría.")]
        [Display(Name = "Categoría")]
        public int? CategoriaId { get; set; }

        [Required(ErrorMessage = "Seleccione una marca.")]
        [Display(Name = "Marca")]
        public int? MarcaId { get; set; }

        [Required(ErrorMessage = "Seleccione un proveedor.")]
        [Display(Name = "Proveedor")]
        public int? ProveedorId { get; set; }

        // Imagen actual guardada (solo lectura en el formulario)
        public string? ImagenRuta { get; set; }

        // Archivo nuevo adjunto y opción de quitar la imagen existente
        [Display(Name = "Imagen del producto")]
        public IFormFile? ImagenArchivo { get; set; }

        [Display(Name = "Quitar la imagen actual")]
        public bool QuitarImagen { get; set; }

        // Listas desplegables (se cargan en el servidor)
        public IEnumerable<SelectListItem> Categorias { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Marcas { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Proveedores { get; set; } = new List<SelectListItem>();
    }
}