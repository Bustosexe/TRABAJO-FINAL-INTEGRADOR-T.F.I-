using System.ComponentModel.DataAnnotations;

namespace StockSuper.Models
{
    // Rubro del catálogo (lácteos, almacén, limpieza, indumentaria, etc.).
    // Determina si sus productos requieren control de vencimiento.
    public class Categoria : EmpresaEntity
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la categoría es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(250, ErrorMessage = "La descripción no puede superar los 250 caracteres.")]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        // Lácteos y frescos: sí. Indumentaria y limpieza: no.
        // Si es verdadero, el pallet exige fecha de vencimiento al ingresar.
        [Display(Name = "Controla vencimiento")]
        public bool ControlaVencimiento { get; set; }

        public ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }
}