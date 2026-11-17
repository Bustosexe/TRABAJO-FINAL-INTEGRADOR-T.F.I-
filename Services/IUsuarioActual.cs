namespace StockSuper.Services
{
    // Expone la identidad del usuario autenticado (cuenta, empresa y sucursal).
    // El DbContext lo usa para aislar el stock por empresa y sucursal, y para
    // completar la auditoría automáticamente.
    public interface IUsuarioActual
    {
        string? UsuarioId { get; }
        int? EmpresaId { get; }
        int? SucursalId { get; }

        // Nombre visible de la cuenta y dirección IP de origen, para la bitácora de auditoría
        string? NombreCuenta { get; }
        string? DireccionIp { get; }
    }
}