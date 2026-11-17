namespace StockSuper.Models.ViewModels
{
    // Fila del listado de productos, con el stock calculado en la base de datos.
    // El stock de un producto es la suma de CantidadActual de sus pallets; como el
    // filtro global limita los pallets, cada usuario ve el stock de su alcance
    // (la sucursal, o todas las de su empresa en el caso del director).
    public class ProductoListadoViewModel
    {
        public int Id { get; set; }
        public string CodigoSku { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string Proveedor { get; set; } = string.Empty;
        public decimal PrecioVenta { get; set; }
        public int StockMinimo { get; set; }
        public int StockActual { get; set; }
        public bool Activo { get; set; }
        public string? ImagenRuta { get; set; }

        // Datos crudos de auditoría; la última acción se arma en el controlador
        public string? UsuarioCreacionId { get; set; }
        public string? UsuarioModificacionId { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public string UltimaAccion { get; set; } = string.Empty;
    }

    // Pallet con existencia de un producto, para la pantalla de detalle
    public class PalletDetalleItem
    {
        public string Sucursal { get; set; } = string.Empty;
        public string CodigoPallet { get; set; } = string.Empty;
        public string Sector { get; set; } = string.Empty;
        public string? Pasillo { get; set; }
        public string? Estante { get; set; }
        public int CantidadActual { get; set; }
        public DateTime? FechaVencimiento { get; set; }
    }

    public class ProductoDetalleViewModel
    {
        public Producto Producto { get; set; } = null!;
        public int StockTotal { get; set; }
        public List<PalletDetalleItem> Pallets { get; set; } = new();
        public PieAuditoriaViewModel Auditoria { get; set; } = new();
    }
}