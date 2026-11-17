namespace StockSuper.Models.Enums
{
    // Las sucursales nunca se eliminan: si cierran, pasan a estado Cerrada
    // y conservan todo su historial de stock y movimientos.
    public enum EstadoSucursal
    {
        Activa = 1,
        Cerrada = 2
    }
}