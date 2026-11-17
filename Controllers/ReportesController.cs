using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StockSuper.Data;
using StockSuper.Models;
using StockSuper.Models.Enums;
using StockSuper.Models.ViewModels;
using StockSuper.Services;

namespace StockSuper.Controllers
{
    // Reportes de stock. El stock inicial, las entradas, las salidas y el stock final
    // se calculan a partir de los movimientos, que son inmutables: el reporte de
    // cualquier fecha pasada siempre da el mismo resultado. Se puede ver en pantalla,
    // descargar en PDF o enviar por correo, siempre con el mismo cálculo.
    [Authorize(Roles = Roles.DirectorEmpresa + "," + Roles.UsuarioSucursal)]
    public class ReportesController : Controller
    {
        private readonly ApplicationDbContext _contexto;
        private readonly IUsuarioActual _usuarioActual;
        private readonly ServicioPdfReporte _pdf;
        private readonly ServicioCorreo _correo;
        private readonly ILogger<ReportesController> _logger;
        private readonly ServicioEventosAuditoria _eventos;

        public ReportesController(
            ApplicationDbContext contexto, IUsuarioActual usuarioActual,
            ServicioPdfReporte pdf, ServicioCorreo correo, ILogger<ReportesController> logger,
            ServicioEventosAuditoria eventos)
        {
            _eventos = eventos;
            _contexto = contexto;
            _usuarioActual = usuarioActual;
            _pdf = pdf;
            _correo = correo;
            _logger = logger;
        }

        public async Task<IActionResult> Stock(
            DateTime? desde, DateTime? hasta, int? sucursalId, int? categoriaId, string? buscar)
        {
            var modelo = await ConstruirAsync(desde, hasta, sucursalId, categoriaId, buscar);

            modelo.Categorias = new SelectList(
                await _contexto.Categorias.AsNoTracking().OrderBy(c => c.Nombre).ToListAsync(),
                "Id", "Nombre", categoriaId);

            // El selector de sucursal solo tiene sentido para la Dirección
            if (!_usuarioActual.SucursalId.HasValue)
            {
                modelo.Sucursales = new SelectList(
                    await _contexto.Sucursales.AsNoTracking().OrderBy(s => s.Nombre).ToListAsync(),
                    "Id", "Nombre", sucursalId);
            }

            return View(modelo);
        }

        // Descarga del reporte en PDF con los mismos filtros de la pantalla
        public async Task<IActionResult> StockPdf(
            DateTime? desde, DateTime? hasta, int? sucursalId, int? categoriaId, string? buscar)
        {
            var modelo = await ConstruirAsync(desde, hasta, sucursalId, categoriaId, buscar);
            var pdf = await GenerarPdfAsync(modelo);
            return File(pdf, "application/pdf", NombreArchivo(modelo));
        }

        // Envío del reporte en PDF por correo electrónico
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarStock(
            string? destino, DateTime? desde, DateTime? hasta,
            int? sucursalId, int? categoriaId, string? buscar)
        {
            var modelo = await ConstruirAsync(desde, hasta, sucursalId, categoriaId, buscar);
            var filtros = new
            {
                desde = modelo.Desde.ToString("yyyy-MM-dd"),
                hasta = modelo.Hasta.ToString("yyyy-MM-dd"),
                sucursalId,
                categoriaId,
                buscar
            };

            destino = destino?.Trim();

            if (!_correo.Configurado)
            {
                TempData["Error"] = "El envío de correos todavía no está configurado en el servidor.";
                return RedirectToAction(nameof(Stock), filtros);
            }

            if (string.IsNullOrWhiteSpace(destino) || destino.Length > 100
                || !new EmailAddressAttribute().IsValid(destino))
            {
                TempData["Error"] = "Ingrese un correo electrónico de destino válido.";
                return RedirectToAction(nameof(Stock), filtros);
            }

            var pdf = await GenerarPdfAsync(modelo);
            var (empresa, alcance, cuenta) = await DatosContextoAsync(modelo);

            var cuerpo = $"Se adjunta el reporte de stock de {empresa}.\n\n" +
                         $"{alcance}\n" +
                         $"Período: {modelo.Desde:dd/MM/yyyy} al {modelo.Hasta:dd/MM/yyyy}\n\n" +
                         $"Enviado desde StockSuper por la cuenta {cuenta}.";

            try
            {
                await _correo.EnviarAsync(
                    destino, $"Reporte de stock - {empresa}", cuerpo, NombreArchivo(modelo), pdf);

                // Queda en el registro del servidor quién envió qué y a quién
                _logger.LogInformation(
                    "Reporte de stock enviado por {Cuenta} a {Destino}", cuenta, destino);
                await _eventos.RegistrarAsync(AmbitoAuditoria.Negocio, "Reporte de stock enviado por correo",
                    $"Destino: {destino}. Período del {modelo.Desde:dd/MM/yyyy} al {modelo.Hasta:dd/MM/yyyy}", "Reporte");
                TempData["Exito"] = $"El reporte se envió correctamente a {destino}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo enviar el reporte de stock a {Destino}", destino);
                TempData["Error"] = "No se pudo enviar el correo. Verifique la dirección e intente nuevamente.";
            }

            return RedirectToAction(nameof(Stock), filtros);
        }

        // Cálculo compartido por la pantalla, el PDF y el correo
        private async Task<ReporteStockViewModel> ConstruirAsync(
            DateTime? desde, DateTime? hasta, int? sucursalId, int? categoriaId, string? buscar)
        {
            // Por defecto: desde el primer día del mes hasta hoy (fecha de Argentina)
            var hoy = DateTime.UtcNow.AHoraArgentina().Date;
            var fechaDesde = (desde ?? new DateTime(hoy.Year, hoy.Month, 1)).Date;
            var fechaHasta = (hasta ?? hoy).Date;
            if (fechaHasta < fechaDesde) (fechaDesde, fechaHasta) = (fechaHasta, fechaDesde);

            // Las fechas del filtro son de Argentina (UTC-3); en la base están en UTC
            var inicioUtc = fechaDesde.AddHours(3);
            var finUtc = fechaHasta.AddDays(1).AddHours(3);

            // Solo importan los movimientos hasta el cierre del período
            var movimientos = _contexto.MovimientosStock.AsNoTracking()
                .Where(m => m.FechaMovimiento < finUtc);
            if (sucursalId.HasValue)
                movimientos = movimientos.Where(m => m.SucursalId == sucursalId);

            // Una sola consulta agrupada por producto: acumulado previo y movimientos del período
            var acumulados = await movimientos
                .GroupBy(m => m.Pallet!.ProductoId)
                .Select(g => new
                {
                    ProductoId = g.Key,
                    EntradasPrevias = g.Where(m => m.FechaMovimiento < inicioUtc
                        && m.TipoMovimiento == TipoMovimiento.Entrada).Sum(m => (int?)m.Cantidad) ?? 0,
                    SalidasPrevias = g.Where(m => m.FechaMovimiento < inicioUtc
                        && m.TipoMovimiento == TipoMovimiento.Salida).Sum(m => (int?)m.Cantidad) ?? 0,
                    Entradas = g.Where(m => m.FechaMovimiento >= inicioUtc
                        && m.TipoMovimiento == TipoMovimiento.Entrada).Sum(m => (int?)m.Cantidad) ?? 0,
                    Salidas = g.Where(m => m.FechaMovimiento >= inicioUtc
                        && m.TipoMovimiento == TipoMovimiento.Salida).Sum(m => (int?)m.Cantidad) ?? 0
                })
                .ToDictionaryAsync(x => x.ProductoId);

            var catalogo = _contexto.Productos.AsNoTracking().AsQueryable();
            if (categoriaId.HasValue)
                catalogo = catalogo.Where(p => p.CategoriaId == categoriaId);
            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var texto = buscar.Trim();
                catalogo = catalogo.Where(p => p.Nombre.Contains(texto) || p.CodigoSku.Contains(texto));
            }

            var productos = await catalogo.OrderBy(p => p.Nombre)
                .Select(p => new { p.Id, p.CodigoSku, p.Nombre, Categoria = p.Categoria!.Nombre })
                .ToListAsync();

            var filas = new List<ReporteStockFila>();
            foreach (var p in productos)
            {
                if (!acumulados.TryGetValue(p.Id, out var a)) continue;

                var fila = new ReporteStockFila
                {
                    ProductoId = p.Id,
                    Sku = p.CodigoSku,
                    Producto = p.Nombre,
                    Categoria = p.Categoria,
                    StockInicial = a.EntradasPrevias - a.SalidasPrevias,
                    Entradas = a.Entradas,
                    Salidas = a.Salidas
                };

                // Se omiten los productos sin existencia ni actividad en el período
                if (fila.StockInicial != 0 || fila.Entradas != 0 || fila.Salidas != 0 || fila.StockFinal != 0)
                    filas.Add(fila);
            }

            return new ReporteStockViewModel
            {
                Desde = fechaDesde,
                Hasta = fechaHasta,
                Hoy = hoy,
                SucursalId = sucursalId,
                CategoriaId = categoriaId,
                Buscar = buscar,
                Filas = filas
            };
        }

        // Empresa, alcance (sucursal o todas) y cuenta que figuran en el PDF y el correo
        private async Task<(string Empresa, string Alcance, string Cuenta)> DatosContextoAsync(
            ReporteStockViewModel modelo)
        {
            var empresaId = _usuarioActual.EmpresaId;
            var empresa = await _contexto.Empresas.AsNoTracking()
                .Where(e => e.Id == empresaId).Select(e => e.RazonSocial)
                .FirstOrDefaultAsync() ?? "Empresa";

            // El usuario de sucursal siempre ve la suya; la Dirección, la que filtró (o todas)
            var sucursalId = _usuarioActual.SucursalId ?? modelo.SucursalId;
            var alcance = "Todas las sucursales";
            if (sucursalId.HasValue)
            {
                var nombre = await _contexto.Sucursales.AsNoTracking()
                    .Where(s => s.Id == sucursalId).Select(s => s.Nombre).FirstOrDefaultAsync();
                if (nombre != null) alcance = "Sucursal: " + nombre;
            }

            var cuenta = User.FindFirst(NombresClaim.NombreCuenta)?.Value ?? User.Identity?.Name ?? "Sistema";
            return (empresa, alcance, cuenta);
        }

        private async Task<byte[]> GenerarPdfAsync(ReporteStockViewModel modelo)
        {
            var (empresa, alcance, cuenta) = await DatosContextoAsync(modelo);
            return _pdf.GenerarReporteStock(modelo, empresa, alcance, cuenta);
        }

        private static string NombreArchivo(ReporteStockViewModel m) =>
            $"reporte-stock-{m.Desde:yyyyMMdd}-{m.Hasta:yyyyMMdd}.pdf";
    }
}