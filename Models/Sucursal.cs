using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StockSuper.Models.Enums;

namespace StockSuper.Models
{
    // Local físico de una empresa. Toda sucursal tiene ubicación geográfica
    // obligatoria, ya que no existe una sucursal sin dirección real.
    // Nunca se elimina: se la marca como Cerrada (baja lógica).
    public class Sucursal : EmpresaEntity
    {
        public int Id { get; set; }

        // Número identificador dentro de la empresa (ej.: "202", "11")
        [Required(ErrorMessage = "El código de sucursal es obligatorio.")]
        [StringLength(10, ErrorMessage = "El código no puede superar los 10 caracteres.")]
        [Display(Name = "Código de sucursal")]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre de la sucursal es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = string.Empty;

        // Casa central: despacha pallets hacia el resto de las sucursales
        [Display(Name = "Casa central")]
        public bool EsCasaCentral { get; set; }

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

        // Opcionales: la ubicación se resuelve con la dirección real en Google Maps.
        // Las columnas se conservan por si más adelante se incrusta un mapa.
        [Column(TypeName = "decimal(9,6)")]
        public decimal? Latitud { get; set; }

        [Column(TypeName = "decimal(9,6)")]
        public decimal? Longitud { get; set; }

        [Display(Name = "Estado")]
        public EstadoSucursal Estado { get; set; } = EstadoSucursal.Activa;

        public ICollection<SectorDeposito> Sectores { get; set; } = new List<SectorDeposito>();
        public ICollection<Pallet> Pallets { get; set; } = new List<Pallet>();
    }
}