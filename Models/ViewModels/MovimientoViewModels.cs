using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using StockSuper.Models.Enums;

namespace StockSuper.Models.ViewModels
{
    // Motivo ofrecido en el formulario y los tipos de movimiento en que aplica ("1", "2" o "1,2")
    public class MotivoOpcion
    {
        public MotivoMovimiento Valor { get; set; }
        public string Texto { get; set; } = string.Empty;
        public string Tipos { get; set; } = string.Empty;
    }

    // Formulario de registro de un movimiento. Sucursal, empresa, usuario y fecha
    // los completa el servidor: el navegador no puede enviarlos.
    public class MovimientoFormViewModel
    {
        [Required(ErrorMessage = "Seleccione un pallet.")]
        [Display(Name = "Pallet")]
        public int? PalletId { get; set; }

        [Required(ErrorMessage = "Seleccione el tipo de movimiento.")]
        [Display(Name = "Tipo de movimiento")]
        public TipoMovimiento? TipoMovimiento { get; set; }

        [Required(ErrorMessage = "Seleccione el motivo.")]
        [Display(Name = "Motivo")]
        public MotivoMovimiento? Motivo { get; set; }

        [Required(ErrorMessage = "La cantidad es obligatoria.")]
        [Range(1, 1000000, ErrorMessage = "La cantidad debe ser un número entero mayor a cero.")]
        [Display(Name = "Cantidad (unidades)")]
        public int? Cantidad { get; set; }

        [StringLength(250, ErrorMessage = "La observación no puede superar los 250 caracteres.")]
        [Display(Name = "Observación")]
        public string? Observacion { get; set; }

        public IEnumerable<SelectListItem> Pallets { get; set; } = new List<SelectListItem>();
        public List<MotivoOpcion> Motivos { get; set; } = new();

        // Id de pallet -> unidades disponibles (lo usa el JavaScript del formulario)
        public Dictionary<int, int> StockPorPallet { get; set; } = new();
    }

    // Fila del historial de movimientos
    public class MovimientoListadoViewModel
    {
        public int Id { get; set; }
        public DateTime FechaMovimiento { get; set; }
        public string Sucursal { get; set; } = string.Empty;
        public int PalletId { get; set; }
        public string CodigoPallet { get; set; } = string.Empty;
        public string Producto { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public TipoMovimiento Tipo { get; set; }
        public MotivoMovimiento Motivo { get; set; }
        public int Cantidad { get; set; }
        public string? Observacion { get; set; }
        public string Cuenta { get; set; } = string.Empty;
    }
}