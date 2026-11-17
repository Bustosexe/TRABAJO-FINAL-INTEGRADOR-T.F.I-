using StockSuper.Models.Enums;

namespace StockSuper.Services
{
    // Combinaciones válidas entre el sentido del movimiento y su motivo.
    // Una venta nunca puede ser una entrada, ni un ingreso de mercadería una salida.
    public static class ReglasMovimiento
    {
        private static readonly Dictionary<TipoMovimiento, MotivoMovimiento[]> Permitidos = new()
        {
            [TipoMovimiento.Entrada] = new[]
            {
                MotivoMovimiento.IngresoMercaderia,
                MotivoMovimiento.AjusteInventario,
                MotivoMovimiento.Otro
            },
            [TipoMovimiento.Salida] = new[]
            {
                MotivoMovimiento.Venta,
                MotivoMovimiento.Rotura,
                MotivoMovimiento.Vencimiento,
                MotivoMovimiento.AjusteInventario,
                MotivoMovimiento.Otro
            }
        };

        public static bool MotivoPermitido(TipoMovimiento tipo, MotivoMovimiento motivo) =>
            Permitidos.TryGetValue(tipo, out var motivos) && motivos.Contains(motivo);

        // Motivos ofrecidos en el formulario, con los tipos en que aplica cada uno
        public static IEnumerable<(MotivoMovimiento Motivo, string Tipos)> Opciones() =>
            Permitidos.SelectMany(p => p.Value.Select(m => (Motivo: m, Tipo: (int)p.Key)))
                .GroupBy(x => x.Motivo)
                .Select(g => (g.Key, string.Join(",", g.Select(x => x.Tipo))));
    }
}