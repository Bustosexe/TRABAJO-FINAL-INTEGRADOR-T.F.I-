using StockSuper.Models.Enums;

namespace StockSuper.Services
{
    // Textos legibles de las enumeraciones para mostrar en pantalla
    public static class EtiquetasEnum
    {
        public static string Etiqueta(this TipoMovimiento tipo) => tipo switch
        {
            TipoMovimiento.Entrada => "Entrada",
            TipoMovimiento.Salida => "Salida",
            _ => tipo.ToString()
        };

        public static string Etiqueta(this MotivoMovimiento motivo) => motivo switch
        {
            MotivoMovimiento.IngresoMercaderia => "Ingreso de mercadería",
            MotivoMovimiento.Venta => "Venta",
            MotivoMovimiento.Rotura => "Rotura",
            MotivoMovimiento.Vencimiento => "Vencimiento",
            MotivoMovimiento.AjusteInventario => "Ajuste de inventario",
            MotivoMovimiento.EnvioASucursal => "Envío a sucursal",
            _ => "Otro"
        };
    }
}