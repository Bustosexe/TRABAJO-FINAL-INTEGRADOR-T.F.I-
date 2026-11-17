using System.ComponentModel.DataAnnotations;

namespace StockSuper.Models
{
    // Marca comercial del producto, propia del catálogo de cada empresa
    public class Marca : EmpresaEntity
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de la marca es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        public ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }
}