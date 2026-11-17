using System.ComponentModel.DataAnnotations;

namespace StockSuper.Models
{
    // Lugar físico dentro del depósito de UNA sucursal (sector, pasillo, estante).
    // Solo los pallets tienen ubicación; los productos del catálogo no.
    public class SectorDeposito : SucursalEntity
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del sector es obligatorio.")]
        [StringLength(50, ErrorMessage = "El sector no puede superar los 50 caracteres.")]
        [Display(Name = "Sector")]
        public string NombreSector { get; set; } = string.Empty;

        [StringLength(10, ErrorMessage = "El pasillo no puede superar los 10 caracteres.")]
        [Display(Name = "Pasillo")]
        public string? Pasillo { get; set; }

        [StringLength(10, ErrorMessage = "El estante no puede superar los 10 caracteres.")]
        [Display(Name = "Estante")]
        public string? Estante { get; set; }

        public ICollection<Pallet> Pallets { get; set; } = new List<Pallet>();
    }
}