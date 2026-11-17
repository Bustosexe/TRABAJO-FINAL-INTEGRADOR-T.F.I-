namespace StockSuper.Models.Enums
{
    // Causa del movimiento. Es informativa: para el stock solo importa
    // que las unidades entraron o salieron, no por qué medio.
    public enum MotivoMovimiento
    {
        IngresoMercaderia = 1,   // Recepción de compra a proveedor
        Venta = 2,               // Salida por venta al público
        Rotura = 3,              // Producto dañado
        Vencimiento = 4,         // Producto vencido retirado del stock
        AjusteInventario = 5,    // Corrección tras conteo físico
        EnvioASucursal = 6,      // Despacho desde casa central
        Otro = 7
    }
}