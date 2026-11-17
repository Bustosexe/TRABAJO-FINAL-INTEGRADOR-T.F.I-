using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StockSuper.Models
{
    // Unidad física de mercadería almacenada en una sucursal. Es lo que tiene
    // ubicación, cantidad, costo y vencimiento. El stock de un producto en una
    // sucursal es la suma de CantidadActual de sus pallets.
    public class Pallet : SucursalEntity
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El código de pallet es obligatorio.")]
        [StringLength(50, ErrorMessage = "El código de pallet no puede superar los 50 caracteres.")]
        [Display(Name = "Código de pallet")]
        public string CodigoPallet { get; set; } = string.Empty;

        // Lote de fabricación informado por el proveedor
        [StringLength(50, ErrorMessage = "El lote no puede superar los 50 caracteres.")]
        [Display(Name = "Lote del proveedor")]
        public string? CodigoLote { get; set; }

        [Required(ErrorMessage = "Seleccione un producto.")]
        [Display(Name = "Producto")]
        public int ProductoId { get; set; }

        [Required(ErrorMessage = "Seleccione un sector de depósito.")]
        [Display(Name = "Ubicación en depósito")]
        public int SectorDepositoId { get; set; }

        [Required(ErrorMessage = "La cantidad inicial es obligatoria.")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad inicial debe ser mayor a cero.")]
        [Display(Name = "Cantidad inicial")]
        public int CantidadInicial { get; set; }

        // Existencia remanente: la modifican únicamente los movimientos de stock
        [Display(Name = "Cantidad actual")]
        public int CantidadActual { get; set; }

        [Required(ErrorMessage = "El precio de compra es obligatorio.")]
        [Range(0.01, 99999999, ErrorMessage = "El precio de compra debe ser mayor a cero.")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Precio de compra por unidad")]
        public decimal PrecioCompra { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Fecha de elaboración")]
        public DateTime? FechaElaboracion { get; set; }

        // Obligatoria solo si la categoría del producto controla vencimiento
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de vencimiento")]
        public DateTime? FechaVencimiento { get; set; }

        // Remito del proveedor en PDF (opcional). El archivo se guarda en una carpeta privada del
        // servidor, fuera de wwwroot: solo se descarga mediante PalletsController.Remito, que
        // verifica que el pallet pertenezca a la empresa y sucursal de quien lo pide.
        [StringLength(300)]
        public string? RemitoRuta { get; set; }

        [StringLength(150)]
        [Display(Name = "Remito")]
        public string? RemitoNombreOriginal { get; set; }

        public Producto? Producto { get; set; }
        public SectorDeposito? SectorDeposito { get; set; }

        public ICollection<MovimientoStock> Movimientos { get; set; } = new List<MovimientoStock>();

        // Control de concurrencia: SQL Server cambia este valor en cada actualización.
        // Si dos usuarios modifican el mismo pallet a la vez, el segundo guardado falla
        // en lugar de pisar la cantidad del primero.
        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}