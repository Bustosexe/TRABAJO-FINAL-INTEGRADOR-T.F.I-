using System.Text;
using Microsoft.EntityFrameworkCore;
using StockSuper.Data;
using StockSuper.Models.Enums;

namespace StockSuper.Services
{
    // Arma el resumen de stock que recibe el asistente de IA como contexto. Todas las
    // consultas pasan por los filtros globales del DbContext, por lo que el asistente
    // solo conoce lo que la cuenta ya puede ver: su empresa y, si corresponde, su sucursal.
    // Los topes de cada lista mantienen acotado el tamaño del prompt (límite de tokens
    // del nivel gratuito del proveedor de IA).
    public class ServicioContextoStock
    {
        private const int MaximoProductos = 30;
        private const int MaximoAlertas = 10;
        private const int MaximoMovimientos = 8;

        private readonly ApplicationDbContext _contexto;
        private readonly IUsuarioActual _usuarioActual;

        public ServicioContextoStock(ApplicationDbContext contexto, IUsuarioActual usuarioActual)
        {
            _contexto = contexto;
            _usuarioActual = usuarioActual;
        }

        public async Task<string> ArmarAsync(CancellationToken cancelacion = default)
        {
            var hoy = FormatoFecha.HoyArgentina;
            var limite = hoy.AddDays(30);
            var texto = new StringBuilder();

            var empresaId = _usuarioActual.EmpresaId;
            var empresa = await _contexto.Empresas.AsNoTracking()
                .Where(e => e.Id == empresaId).Select(e => e.RazonSocial)
                .FirstOrDefaultAsync(cancelacion) ?? "la empresa";

            var alcance = "todas las sucursales de la empresa";
            if (_usuarioActual.SucursalId.HasValue)
            {
                var sucursal = await _contexto.Sucursales.AsNoTracking()
                    .Select(s => s.Nombre).FirstOrDefaultAsync(cancelacion);
                if (sucursal != null) alcance = "sucursal " + sucursal;
            }

            texto.AppendLine($"Empresa: {empresa}. Alcance de los datos: {alcance}. Fecha de hoy: {hoy:dd/MM/yyyy}.");

            var unidades = await _contexto.Pallets.SumAsync(p => p.CantidadActual, cancelacion);
            var palletsConStock = await _contexto.Pallets.CountAsync(p => p.CantidadActual > 0, cancelacion);
            texto.AppendLine($"Totales: {unidades} unidades en stock, en {palletsConStock} pallets con existencia.");
            texto.AppendLine();

            // Stock por producto (suma de sus pallets) y su umbral de reposición
            var productos = await _contexto.Productos.AsNoTracking()
                .Where(p => p.Activo)
                .OrderBy(p => p.Nombre)
                .Select(p => new
                {
                    p.Nombre,
                    p.CodigoSku,
                    Categoria = p.Categoria!.Nombre,
                    Proveedor = p.Proveedor!.RazonSocial,
                    p.StockMinimo,
                    Stock = p.Pallets.Sum(x => x.CantidadActual)
                })
                .Take(MaximoProductos)
                .ToListAsync(cancelacion);

            texto.AppendLine($"STOCK POR PRODUCTO (hasta {MaximoProductos} productos activos):");
            foreach (var p in productos)
            {
                var situacion = p.Stock < p.StockMinimo ? ", BAJO EL MÍNIMO" : string.Empty;
                texto.AppendLine($"- {p.Nombre} (SKU {p.CodigoSku}, {p.Categoria}, proveedor {p.Proveedor}): stock {p.Stock}, mínimo {p.StockMinimo}{situacion}");
            }
            if (productos.Count == 0) texto.AppendLine("- No hay productos activos cargados.");
            texto.AppendLine();

            var conVencimiento = _contexto.Pallets.AsNoTracking()
                .Where(p => p.CantidadActual > 0 && p.FechaVencimiento != null);

            var vencidos = await conVencimiento
                .Where(p => p.FechaVencimiento < hoy)
                .OrderBy(p => p.FechaVencimiento)
                .Take(MaximoAlertas)
                .Select(p => new
                {
                    p.CodigoPallet,
                    Producto = p.Producto!.Nombre,
                    Sucursal = p.Sucursal!.Nombre,
                    Sector = p.SectorDeposito!.NombreSector,
                    p.CantidadActual,
                    Vence = p.FechaVencimiento!.Value
                })
                .ToListAsync(cancelacion);

            texto.AppendLine("PALLETS VENCIDOS CON EXISTENCIA:");
            foreach (var p in vencidos)
                texto.AppendLine($"- Pallet {p.CodigoPallet}, {p.Producto}, {p.Sucursal}, sector {p.Sector}: {p.CantidadActual} unidades, venció el {p.Vence:dd/MM/yyyy}");
            if (vencidos.Count == 0) texto.AppendLine("- Ninguno.");
            texto.AppendLine();

            var porVencer = await conVencimiento
                .Where(p => p.FechaVencimiento >= hoy && p.FechaVencimiento <= limite)
                .OrderBy(p => p.FechaVencimiento)
                .Take(MaximoAlertas)
                .Select(p => new
                {
                    p.CodigoPallet,
                    Producto = p.Producto!.Nombre,
                    Sucursal = p.Sucursal!.Nombre,
                    Sector = p.SectorDeposito!.NombreSector,
                    p.CantidadActual,
                    Vence = p.FechaVencimiento!.Value
                })
                .ToListAsync(cancelacion);

            texto.AppendLine("PALLETS QUE VENCEN EN LOS PRÓXIMOS 30 DÍAS:");
            foreach (var p in porVencer)
                texto.AppendLine($"- Pallet {p.CodigoPallet}, {p.Producto}, {p.Sucursal}, sector {p.Sector}: {p.CantidadActual} unidades, vence el {p.Vence:dd/MM/yyyy}");
            if (porVencer.Count == 0) texto.AppendLine("- Ninguno.");
            texto.AppendLine();

            var movimientos = await _contexto.MovimientosStock.AsNoTracking()
                .OrderByDescending(m => m.FechaMovimiento).ThenByDescending(m => m.Id)
                .Take(MaximoMovimientos)
                .Select(m => new
                {
                    m.FechaMovimiento,
                    m.TipoMovimiento,
                    m.Motivo,
                    m.Cantidad,
                    Producto = m.Pallet!.Producto!.Nombre,
                    Sucursal = m.Pallet.Sucursal!.Nombre
                })
                .ToListAsync(cancelacion);

            texto.AppendLine("ÚLTIMOS MOVIMIENTOS DE STOCK:");
            foreach (var m in movimientos)
            {
                var signo = m.TipoMovimiento == TipoMovimiento.Entrada ? "+" : "-";
                texto.AppendLine($"- {m.FechaMovimiento.AHoraArgentina():dd/MM/yyyy HH:mm}, {m.Sucursal}: {m.TipoMovimiento.Etiqueta()} por {m.Motivo.Etiqueta()}, {signo}{m.Cantidad} unidades de {m.Producto}");
            }
            if (movimientos.Count == 0) texto.AppendLine("- Sin movimientos registrados.");

            return texto.ToString();
        }
    }
}
