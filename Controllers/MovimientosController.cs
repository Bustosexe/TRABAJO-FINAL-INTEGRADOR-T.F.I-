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
    // Movimientos de stock: toda entrada o salida de unidades de un pallet.
    // La Dirección consulta el historial de todas las sucursales de su empresa;
    // el usuario de cada sucursal registra los movimientos de la suya.
    // Los movimientos nunca se editan ni se eliminan: un error se corrige con otro movimiento.
    [Authorize(Roles = Roles.DirectorEmpresa + "," + Roles.UsuarioSucursal)]
    public class MovimientosController : Controller
    {
        private const int MaximoFilas = 200;

        private readonly ApplicationDbContext _contexto;
        private readonly IUsuarioActual _usuarioActual;

        public MovimientosController(ApplicationDbContext contexto, IUsuarioActual usuarioActual)
        {
            _contexto = contexto;
            _usuarioActual = usuarioActual;
        }

        // Historial con filtros por fecha, tipo, motivo y sucursal, y totales del período
        public async Task<IActionResult> Index(
            string? buscar, DateTime? desde, DateTime? hasta,
            TipoMovimiento? tipo, MotivoMovimiento? motivo, int? sucursalId)
        {
            var consulta = _contexto.MovimientosStock.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var texto = buscar.Trim();
                consulta = consulta.Where(m => m.Pallet!.CodigoPallet.Contains(texto)
                    || m.Pallet.Producto!.Nombre.Contains(texto)
                    || m.Pallet.Producto.CodigoSku.Contains(texto));
            }

            // Las fechas del filtro son de Argentina (UTC-3) y en la base están en UTC
            if (desde.HasValue)
            {
                var inicioUtc = desde.Value.Date.AddHours(3);
                consulta = consulta.Where(m => m.FechaMovimiento >= inicioUtc);
            }
            if (hasta.HasValue)
            {
                var finUtc = hasta.Value.Date.AddDays(1).AddHours(3);
                consulta = consulta.Where(m => m.FechaMovimiento < finUtc);
            }

            if (tipo.HasValue) consulta = consulta.Where(m => m.TipoMovimiento == tipo);
            if (motivo.HasValue) consulta = consulta.Where(m => m.Motivo == motivo);
            if (sucursalId.HasValue) consulta = consulta.Where(m => m.SucursalId == sucursalId);

            // Totales sobre todo el resultado filtrado, no solo sobre las filas mostradas
            var totalEntradas = await consulta.Where(m => m.TipoMovimiento == TipoMovimiento.Entrada)
                .SumAsync(m => (int?)m.Cantidad) ?? 0;
            var totalSalidas = await consulta.Where(m => m.TipoMovimiento == TipoMovimiento.Salida)
                .SumAsync(m => (int?)m.Cantidad) ?? 0;
            var cantidadMovimientos = await consulta.CountAsync();

            var lista = await consulta
                .OrderByDescending(m => m.FechaMovimiento).ThenByDescending(m => m.Id)
                .Take(MaximoFilas)
                .Select(m => new MovimientoListadoViewModel
                {
                    Id = m.Id,
                    FechaMovimiento = m.FechaMovimiento,
                    Sucursal = m.Pallet!.Sucursal!.Nombre,
                    PalletId = m.PalletId,
                    CodigoPallet = m.Pallet.CodigoPallet,
                    Producto = m.Pallet.Producto!.Nombre,
                    Sku = m.Pallet.Producto.CodigoSku,
                    Tipo = m.TipoMovimiento,
                    Motivo = m.Motivo,
                    Cantidad = m.Cantidad,
                    Observacion = m.Observacion,
                    Cuenta = m.Usuario!.NombreCuenta
                }).ToListAsync();

            if (!_usuarioActual.SucursalId.HasValue)
            {
                ViewData["Sucursales"] = new SelectList(
                    await _contexto.Sucursales.AsNoTracking().OrderBy(s => s.Nombre).ToListAsync(),
                    "Id", "Nombre", sucursalId);
            }

            ViewData["Buscar"] = buscar;
            ViewData["Desde"] = desde;
            ViewData["Hasta"] = hasta;
            ViewData["Tipo"] = tipo;
            ViewData["Motivo"] = motivo;
            ViewData["SucursalId"] = sucursalId;
            ViewData["TotalEntradas"] = totalEntradas;
            ViewData["TotalSalidas"] = totalSalidas;
            ViewData["Cantidad"] = cantidadMovimientos;
            ViewData["Maximo"] = MaximoFilas;
            return View(lista);
        }

        [Authorize(Roles = Roles.UsuarioSucursal)]
        [HttpGet]
        public async Task<IActionResult> Registrar(int? palletId)
        {
            var modelo = new MovimientoFormViewModel { PalletId = palletId };
            await CargarListasAsync(modelo);
            return View(modelo);
        }

        [Authorize(Roles = Roles.UsuarioSucursal)]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(MovimientoFormViewModel modelo)
        {
            modelo.Observacion = string.IsNullOrWhiteSpace(modelo.Observacion) ? null : modelo.Observacion.Trim();

            // El filtro global asegura que solo se pueda operar un pallet de la propia sucursal
            Pallet? pallet = null;
            if (modelo.PalletId.HasValue)
            {
                pallet = await _contexto.Pallets.Include(p => p.Producto)
                    .FirstOrDefaultAsync(p => p.Id == modelo.PalletId);
                if (pallet == null)
                    ModelState.AddModelError(nameof(modelo.PalletId), "El pallet seleccionado no es válido.");
            }

            if (modelo.TipoMovimiento.HasValue && modelo.Motivo.HasValue &&
                !ReglasMovimiento.MotivoPermitido(modelo.TipoMovimiento.Value, modelo.Motivo.Value))
                ModelState.AddModelError(nameof(modelo.Motivo), "El motivo no corresponde al tipo de movimiento elegido.");

            if (pallet != null && modelo.TipoMovimiento.HasValue)
            {
                var vencido = pallet.FechaVencimiento.HasValue && pallet.FechaVencimiento.Value.Date < FormatoFecha.HoyArgentina;

                if (modelo.TipoMovimiento == TipoMovimiento.Salida
                    && modelo.Cantidad.HasValue && modelo.Cantidad.Value > pallet.CantidadActual)
                    ModelState.AddModelError(nameof(modelo.Cantidad),
                        $"No hay stock suficiente: el pallet tiene {pallet.CantidadActual:N0} unidades disponibles.");

                if (modelo.TipoMovimiento == TipoMovimiento.Entrada && vencido)
                    ModelState.AddModelError(nameof(modelo.PalletId),
                        "No se puede reponer un pallet vencido. Ingrese un pallet nuevo.");

                if (modelo.TipoMovimiento == TipoMovimiento.Salida
                    && modelo.Motivo == MotivoMovimiento.Venta && vencido)
                    ModelState.AddModelError(nameof(modelo.Motivo),
                        "No se puede vender mercadería vencida. Registre la salida como Vencimiento o Rotura.");
            }

            if (!ModelState.IsValid)
            {
                await CargarListasAsync(modelo);
                return View(modelo);
            }

            var tipo = modelo.TipoMovimiento!.Value;
            var cantidad = modelo.Cantidad!.Value;

            // El pallet y su movimiento se guardan juntos en una sola transacción
            pallet!.CantidadActual += tipo == TipoMovimiento.Entrada ? cantidad : -cantidad;

            _contexto.MovimientosStock.Add(new MovimientoStock
            {
                PalletId = pallet.Id,
                FechaMovimiento = DateTime.UtcNow,
                TipoMovimiento = tipo,
                Motivo = modelo.Motivo!.Value,
                Cantidad = cantidad,
                Observacion = modelo.Observacion,
                UsuarioId = _usuarioActual.UsuarioId!
            });

            try
            {
                await _contexto.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Otro equipo modificó este pallet entre la lectura y el guardado
                _contexto.ChangeTracker.Clear();
                ModelState.AddModelError(string.Empty,
                    "Otro equipo modificó este pallet mientras cargaba el movimiento. Verifique la cantidad disponible e intente nuevamente.");
                await CargarListasAsync(modelo);
                return View(modelo);
            }

            TempData["Exito"] = $"{tipo.Etiqueta()} registrada: {cantidad:N0} unidades en el pallet {pallet.CodigoPallet}. Stock actual: {pallet.CantidadActual:N0}.";
            return RedirectToAction("Detalle", "Pallets", new { id = pallet.Id });
        }

        private async Task CargarListasAsync(MovimientoFormViewModel modelo)
        {
            var pallets = await _contexto.Pallets.AsNoTracking()
                .OrderBy(p => p.CodigoPallet)
                .Select(p => new { p.Id, p.CodigoPallet, Producto = p.Producto!.Nombre, p.CantidadActual })
                .ToListAsync();

            modelo.Pallets = pallets
                .Select(p => new SelectListItem($"{p.CodigoPallet} - {p.Producto} (stock: {p.CantidadActual:N0})", p.Id.ToString()))
                .ToList();
            modelo.StockPorPallet = pallets.ToDictionary(p => p.Id, p => p.CantidadActual);

            modelo.Motivos = ReglasMovimiento.Opciones()
                .Select(o => new MotivoOpcion { Valor = o.Motivo, Texto = o.Motivo.Etiqueta(), Tipos = o.Tipos })
                .ToList();
        }
    }
}