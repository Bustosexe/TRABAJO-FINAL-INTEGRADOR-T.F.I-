namespace StockSuper.Models.ViewModels
{
    // Datos del panel de inicio. Un mismo modelo sirve a los tres roles:
    // cada rol completa solo los indicadores que le corresponden.
    public class DashboardViewModel
    {
        // Administrador de plataforma: gestiona clientes, no ve stock
        public bool EsPlataforma { get; set; }
        public int TotalEmpresas { get; set; }
        public int EmpresasActivas { get; set; }
        public int TotalSucursales { get; set; }
        public int TotalCuentas { get; set; }

        // Director y usuario de sucursal: indicadores de stock
        public string NombreEmpresa { get; set; } = string.Empty;
        public string? NombreSucursal { get; set; }   // Nulo para el director
        public int SucursalesActivas { get; set; }     // Solo relevante para el director
        public int ProductosActivos { get; set; }
        public int UnidadesEnStock { get; set; }
        public int PalletsConStock { get; set; }
        public int ProductosBajoMinimo { get; set; }  // Insumos críticos
        public int PalletsPorVencer { get; set; }     // Vencen en los próximos 30 días
        public int PalletsVencidos { get; set; }
    }
}