namespace StockSuper.Models.Enums
{
    // Sentido del movimiento: define si el stock del pallet sube o baja.
    public enum TipoMovimiento
    {
        Entrada = 1,   // Suma unidades al pallet
        Salida = 2     // Resta unidades al pallet
    }
}