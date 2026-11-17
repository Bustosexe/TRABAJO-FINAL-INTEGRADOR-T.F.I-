using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using StockSuper.Models.Enums;

namespace StockSuper.Models.ViewModels
{
    // Ingreso de un pallet nuevo. La cantidad inicial genera automáticamente el
    // primer movimiento de stock (entrada). Sucursal y empresa las asigna el servidor.
    public class PalletFormViewModel
    {
        [Required(ErrorMessage = "El código de pallet es obligatorio.")]
        [StringLength(50, ErrorMessage = "El código de pallet no puede superar los 50 caracteres.")]
        [Display(Name = "Código de pallet")]
        public string CodigoPallet { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "El lote no puede superar los 50 caracteres.")]
        [Display(Name = "Lote del proveedor")]
        public string? CodigoLote { get; set; }

        [Required(ErrorMessage = "Seleccione un producto.")]
        [Display(Name = "Producto")]
        public int? ProductoId { get; set; }

        [Required(ErrorMessage = "Seleccione la ubicación en el depósito.")]
        [Display(Name = "Ubicación en depósito")]
        public int? SectorDepositoId { get; set; }

        [Required(ErrorMessage = "La cantidad inicial es obligatoria.")]
        [Range(1, 1000000, ErrorMessage = "La cantidad inicial debe ser un número entero mayor a cero.")]
        [Display(Name = "Cantidad inicial (unidades)")]
        public int? CantidadInicial { get; set; }

        [Required(ErrorMessage = "El precio de compra es obligatorio.")]
        [Range(0.01, 99999999, ErrorMessage = "El precio de compra debe ser mayor a cero.")]
        [Display(Name = "Precio de compra por unidad")]
        public decimal? PrecioCompra { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Fecha de elaboración")]
        public DateTime? FechaElaboracion { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Fecha de vencimiento")]
        public DateTime? FechaVencimiento { get; set; }

        // Remito del proveedor en PDF (opcional)
        [Display(Name = "Remito del proveedor (PDF)")]
        public IFormFile? RemitoArchivo { get; set; }

        public IEnumerable<SelectListItem> Productos { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Sectores { get; set; } = new List<SelectListItem>();

        // Id de producto -> su categoría exige vencimiento (lo usa el JavaScript del formulario)
        public Dictionary<int, bool> ProductosConVencimiento { get; set; } = new();
    }

    // Edición de un pallet existente. Solo se corrige la ubicación, el lote y las fechas.
    // La cantidad cambia únicamente mediante movimientos, para no perder trazabilidad.
    public class PalletEdicionViewModel
    {
        public int Id { get; set; }

        // Datos de solo lectura
        public string CodigoPallet { get; set; } = string.Empty;
        public string ProductoNombre { get; set; } = string.Empty;
        public int CantidadActual { get; set; }
        public bool ExigeVencimiento { get; set; }

        [Required(ErrorMessage = "Seleccione la ubicación en el depósito.")]
        [Display(Name = "Ubicación en depósito")]
        public int? SectorDepositoId { get; set; }

        [StringLength(50, ErrorMessage = "El lote no puede superar los 50 caracteres.")]
        [Display(Name = "Lote del proveedor")]
        public string? CodigoLote { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Fecha de elaboración")]
        public DateTime? FechaElaboracion { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Fecha de vencimiento")]
        public DateTime? FechaVencimiento { get; set; }

        // Remito del proveedor: se puede adjuntar uno nuevo (reemplaza al actual) o quitar el existente
        [Display(Name = "Remito del proveedor (PDF)")]
        public IFormFile? RemitoArchivo { get; set; }

        [Display(Name = "Quitar el remito actual")]
        public bool QuitarRemito { get; set; }

        // Solo lectura: nombre del remito ya guardado
        public string? RemitoNombre { get; set; }

        public IEnumerable<SelectListItem> Sectores { get; set; } = new List<SelectListItem>();
    }

    // Fila del listado de pallets
    public class PalletListadoViewModel
    {
        public int Id { get; set; }
        public string Sucursal { get; set; } = string.Empty;
        public string CodigoPallet { get; set; } = string.Empty;
        public string? CodigoLote { get; set; }
        public string Producto { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string NombreSector { get; set; } = string.Empty;
        public string? Pasillo { get; set; }
        public string? Estante { get; set; }
        public int CantidadInicial { get; set; }
        public int CantidadActual { get; set; }
        public DateTime? FechaVencimiento { get; set; }

        public string? UsuarioCreacionId { get; set; }
        public string? UsuarioModificacionId { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public string UltimaAccion { get; set; } = string.Empty;
    }

    // Movimiento mostrado en el historial del pallet
    public class MovimientoItem
    {
        public DateTime FechaMovimiento { get; set; }
        public TipoMovimiento Tipo { get; set; }
        public MotivoMovimiento Motivo { get; set; }
        public int Cantidad { get; set; }
        public string? Observacion { get; set; }
        public string Cuenta { get; set; } = string.Empty;
    }

    public class PalletDetalleViewModel
    {
        public Pallet Pallet { get; set; } = null!;
        public List<MovimientoItem> Movimientos { get; set; } = new();
        public PieAuditoriaViewModel Auditoria { get; set; } = new();
    }
}