using Microsoft.AspNetCore.Mvc.Rendering;

namespace StockSuper.Models.ViewModels
{
    // Fila del reporte: movimiento de un producto durante el período elegido
    public class ReporteStockFila
    {
        public int ProductoId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Producto { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;

        // Existencia al comienzo del primer día del período
        public int StockInicial { get; set; }
        public int Entradas { get; set; }
        public int Salidas { get; set; }

        // Stock inicial + entradas - salidas
        public int StockFinal => StockInicial + Entradas - Salidas;
    }

    public class ReporteStockViewModel
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public DateTime Hoy { get; set; }
        public int? SucursalId { get; set; }
        public int? CategoriaId { get; set; }
        public string? Buscar { get; set; }

        public List<ReporteStockFila> Filas { get; set; } = new();
        public IEnumerable<SelectListItem> Sucursales { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Categorias { get; set; } = new List<SelectListItem>();

        public int TotalInicial => Filas.Sum(f => f.StockInicial);
        public int TotalEntradas => Filas.Sum(f => f.Entradas);
        public int TotalSalidas => Filas.Sum(f => f.Salidas);
        public int TotalFinal => Filas.Sum(f => f.StockFinal);
    }

    // Producto cuyo stock total está por debajo del umbral de reposición
    public class AlertaProductoItem
    {
        public int Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public int ProveedorId { get; set; }
        public string Proveedor { get; set; } = string.Empty;
        public int StockMinimo { get; set; }
        public int StockActual { get; set; }
        public int Faltante => Math.Max(0, StockMinimo - StockActual);
    }

    // Pallet con existencia y fecha de vencimiento cercana o vencida
    public class AlertaPalletItem
    {
        public int Id { get; set; }
        public string Sucursal { get; set; } = string.Empty;
        public string CodigoPallet { get; set; } = string.Empty;
        public string Producto { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Sector { get; set; } = string.Empty;
        public int CantidadActual { get; set; }
        public DateTime FechaVencimiento { get; set; }
    }

    public class AlertasViewModel
    {
        public int Dias { get; set; }
        public DateTime Hoy { get; set; }
        public List<AlertaProductoItem> ProductosCriticos { get; set; } = new();
        public List<AlertaPalletItem> PalletsVencidos { get; set; } = new();
        public List<AlertaPalletItem> PalletsPorVencer { get; set; } = new();
    }
}