using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StockSuper.Models
{
    // Catálogo maestro de la empresa: describe el artículo, pero NO tiene
    // ubicación ni existencias. La existencia física vive en los Pallets.
    // El SKU es único dentro de cada empresa (índice compuesto en el DbContext).
    public class Producto : EmpresaEntity
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

        [Required(ErrorMessage = "El precio de venta es obligatorio.")]
        [Range(0.01, 99999999, ErrorMessage = "El precio de venta debe ser mayor a cero.")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Precio de venta")]
        public decimal PrecioVenta { get; set; }

        // Umbral de reposición: si el stock total baja de este valor, es insumo crítico
        [Required(ErrorMessage = "El stock mínimo es obligatorio.")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock mínimo no puede ser negativo.")]
        [Display(Name = "Stock mínimo")]
        public int StockMinimo { get; set; }

        // Ruta de la imagen del producto (archivo adjunto en wwwroot/uploads)
        [StringLength(300)]
        [Display(Name = "Imagen")]
        public string? ImagenRuta { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;

        [Required(ErrorMessage = "Seleccione una categoría.")]
        [Display(Name = "Categoría")]
        public int CategoriaId { get; set; }

        [Required(ErrorMessage = "Seleccione una marca.")]
        [Display(Name = "Marca")]
        public int MarcaId { get; set; }

        [Required(ErrorMessage = "Seleccione un proveedor.")]
        [Display(Name = "Proveedor")]
        public int ProveedorId { get; set; }

        public Categoria? Categoria { get; set; }
        public Marca? Marca { get; set; }
        public Proveedor? Proveedor { get; set; }

        public ICollection<Pallet> Pallets { get; set; } = new List<Pallet>();
    }
}