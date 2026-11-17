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
    // Pallets: unidad física de mercadería con ubicación, cantidad y vencimiento.
    // La Dirección los consulta (todas las sucursales de su empresa); el usuario de
    // cada sucursal los ingresa y corrige. La cantidad solo cambia con movimientos.
    [Authorize(Roles = Roles.DirectorEmpresa + "," + Roles.UsuarioSucursal)]
    public class PalletsController : Controller
    {
        private const int MaximoFilas = 200;

        private readonly ApplicationDbContext _contexto;
        private readonly ServicioAuditoria _auditoria;
        private readonly IUsuarioActual _usuarioActual;
        private readonly ServicioArchivos _archivos;

        public PalletsController(
            ApplicationDbContext contexto, ServicioAuditoria auditoria,
            IUsuarioActual usuarioActual, ServicioArchivos archivos)
        {
            _contexto = contexto;
            _auditoria = auditoria;
            _usuarioActual = usuarioActual;
            _archivos = archivos;
        }

        // Listado con búsqueda, filtro por situación y (para la Dirección) por sucursal.
        // Orden FEFO: primero los que vencen antes, para priorizar su salida.
        public async Task<IActionResult> Index(string? buscar, string? estado, int? sucursalId)
        {
            var hoy = FormatoFecha.HoyArgentina;
            var limiteAviso = hoy.AddDays(30);
            var consulta = _contexto.Pallets.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var texto = buscar.Trim();
                consulta = consulta.Where(p => p.CodigoPallet.Contains(texto)
                    || (p.CodigoLote != null && p.CodigoLote.Contains(texto))
                    || p.Producto!.Nombre.Contains(texto)
                    || p.Producto.CodigoSku.Contains(texto));
            }

            if (sucursalId.HasValue)
                consulta = consulta.Where(p => p.SucursalId == sucursalId);

            consulta = estado switch
            {
                "conexistencia" => consulta.Where(p => p.CantidadActual > 0),
                "agotados" => consulta.Where(p => p.CantidadActual == 0),
                "porvencer" => consulta.Where(p => p.CantidadActual > 0 && p.FechaVencimiento != null
                    && p.FechaVencimiento >= hoy && p.FechaVencimiento <= limiteAviso),
                "vencidos" => consulta.Where(p => p.CantidadActual > 0 && p.FechaVencimiento != null
                    && p.FechaVencimiento < hoy),
                _ => consulta
            };

            var total = await consulta.CountAsync();

            var lista = await consulta
                .OrderBy(p => p.FechaVencimiento == null).ThenBy(p => p.FechaVencimiento).ThenBy(p => p.CodigoPallet)
                .Take(MaximoFilas)
                .Select(p => new PalletListadoViewModel
                {
                    Id = p.Id,
                    Sucursal = p.Sucursal!.Nombre,
                    CodigoPallet = p.CodigoPallet,
                    CodigoLote = p.CodigoLote,
                    Producto = p.Producto!.Nombre,
                    Sku = p.Producto.CodigoSku,
                    NombreSector = p.SectorDeposito!.NombreSector,
                    Pasillo = p.SectorDeposito.Pasillo,
                    Estante = p.SectorDeposito.Estante,
                    CantidadInicial = p.CantidadInicial,
                    CantidadActual = p.CantidadActual,
                    FechaVencimiento = p.FechaVencimiento,
                    UsuarioCreacionId = p.UsuarioCreacionId,
                    UsuarioModificacionId = p.UsuarioModificacionId,
                    FechaCreacion = p.FechaCreacion,
                    FechaModificacion = p.FechaModificacion
                }).ToListAsync();

            var nombres = await _auditoria.NombresAsync(
                lista.SelectMany(i => new[] { i.UsuarioCreacionId, i.UsuarioModificacionId }));
            foreach (var item in lista)
            {
                var usuarioId = item.UsuarioModificacionId ?? item.UsuarioCreacionId;
                var fecha = (item.FechaModificacion ?? item.FechaCreacion).AHoraArgentina();
                var cuenta = usuarioId != null && nombres.TryGetValue(usuarioId, out var n) ? n : "Sistema";
                item.UltimaAccion = $"{cuenta} - {fecha:dd/MM/yyyy HH:mm}";
            }

            // El selector de sucursal solo tiene sentido para la Dirección
            if (!_usuarioActual.SucursalId.HasValue)
            {
                ViewData["Sucursales"] = new SelectList(
                    await _contexto.Sucursales.AsNoTracking().OrderBy(s => s.Nombre).ToListAsync(),
                    "Id", "Nombre", sucursalId);
            }

            ViewData["Buscar"] = buscar;
            ViewData["Estado"] = estado;
            ViewData["SucursalId"] = sucursalId;
            ViewData["Total"] = total;
            ViewData["Maximo"] = MaximoFilas;
            return View(lista);
        }

        // Ficha del pallet con su historial completo de movimientos
        public async Task<IActionResult> Detalle(int id)
        {
            var pallet = await _contexto.Pallets.AsNoTracking()
                .Include(p => p.Producto).Include(p => p.SectorDeposito).Include(p => p.Sucursal)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (pallet == null) return NotFound();

            var movimientos = await _contexto.MovimientosStock.AsNoTracking()
                .Where(m => m.PalletId == id)
                .OrderByDescending(m => m.FechaMovimiento).ThenByDescending(m => m.Id)
                .Select(m => new MovimientoItem
                {
                    FechaMovimiento = m.FechaMovimiento,
                    Tipo = m.TipoMovimiento,
                    Motivo = m.Motivo,
                    Cantidad = m.Cantidad,
                    Observacion = m.Observacion,
                    Cuenta = m.Usuario!.NombreCuenta
                }).ToListAsync();

            return View(new PalletDetalleViewModel
            {
                Pallet = pallet,
                Movimientos = movimientos,
                Auditoria = await _auditoria.ArmarPieAsync(pallet)
            });
        }

        [Authorize(Roles = Roles.UsuarioSucursal)]
        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            var modelo = new PalletFormViewModel();
            await CargarListasAsync(modelo);
            return View("Formulario", modelo);
        }

        [Authorize(Roles = Roles.UsuarioSucursal)]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(PalletFormViewModel modelo)
        {
            modelo.CodigoPallet = modelo.CodigoPallet?.Trim().ToUpperInvariant() ?? string.Empty;
            modelo.CodigoLote = string.IsNullOrWhiteSpace(modelo.CodigoLote) ? null : modelo.CodigoLote.Trim();

            await ValidarIngresoAsync(modelo);

            // Primero se valida todo; el archivo se guarda recién cuando el resto es correcto
            string? remitoRuta = null;
            if (ModelState.IsValid && modelo.RemitoArchivo is { Length: > 0 })
            {
                var (ruta, error) = await _archivos.GuardarRemitoAsync(modelo.RemitoArchivo, _usuarioActual.EmpresaId!.Value);
                if (error != null) ModelState.AddModelError(nameof(modelo.RemitoArchivo), error);
                remitoRuta = ruta;
            }

            if (!ModelState.IsValid)
            {
                await CargarListasAsync(modelo);
                return View("Formulario", modelo);
            }

            var cantidad = modelo.CantidadInicial!.Value;
            var pallet = new Pallet
            {
                CodigoPallet = modelo.CodigoPallet,
                CodigoLote = modelo.CodigoLote,
                ProductoId = modelo.ProductoId!.Value,
                SectorDepositoId = modelo.SectorDepositoId!.Value,
                CantidadInicial = cantidad,
                CantidadActual = cantidad,
                PrecioCompra = modelo.PrecioCompra!.Value,
                FechaElaboracion = modelo.FechaElaboracion?.Date,
                FechaVencimiento = modelo.FechaVencimiento?.Date,
                RemitoRuta = remitoRuta,
                RemitoNombreOriginal = remitoRuta == null ? null : NombreRemito(modelo.RemitoArchivo!.FileName)
            };

            // El ingreso del pallet queda registrado como su primer movimiento. Pallet y
            // movimiento se guardan juntos en una sola transacción: o se guardan ambos o ninguno.
            pallet.Movimientos.Add(new MovimientoStock
            {
                FechaMovimiento = DateTime.UtcNow,
                TipoMovimiento = TipoMovimiento.Entrada,
                Motivo = MotivoMovimiento.IngresoMercaderia,
                Cantidad = cantidad,
                Observacion = "Ingreso inicial del pallet",
                UsuarioId = _usuarioActual.UsuarioId!
            });

            try
            {
                _contexto.Pallets.Add(pallet);
                await _contexto.SaveChangesAsync();
            }
            catch
            {
                // Si falla el guardado no deben quedar archivos huérfanos en el servidor
                _archivos.EliminarRemito(remitoRuta);
                throw;
            }

            TempData["Exito"] = $"El pallet '{pallet.CodigoPallet}' se ingresó correctamente con {cantidad:N0} unidades.";
            return RedirectToAction(nameof(Detalle), new { id = pallet.Id });
        }

        [Authorize(Roles = Roles.UsuarioSucursal)]
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var pallet = await _contexto.Pallets.AsNoTracking()
                .Include(p => p.Producto).ThenInclude(p => p!.Categoria)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (pallet == null) return NotFound();

            var modelo = new PalletEdicionViewModel
            {
                Id = pallet.Id,
                SectorDepositoId = pallet.SectorDepositoId,
                CodigoLote = pallet.CodigoLote,
                FechaElaboracion = pallet.FechaElaboracion,
                FechaVencimiento = pallet.FechaVencimiento
            };
            await CompletarEdicionAsync(modelo, pallet);
            ViewData["Pie"] = await _auditoria.ArmarPieAsync(pallet);
            return View(modelo);
        }

        [Authorize(Roles = Roles.UsuarioSucursal)]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, PalletEdicionViewModel modelo)
        {
            var pallet = await _contexto.Pallets
                .Include(p => p.Producto).ThenInclude(p => p!.Categoria)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (pallet == null) return NotFound();

            modelo.Id = id;
            modelo.CodigoLote = string.IsNullOrWhiteSpace(modelo.CodigoLote) ? null : modelo.CodigoLote.Trim();

            if (modelo.SectorDepositoId.HasValue &&
                !await _contexto.SectoresDeposito.AnyAsync(s => s.Id == modelo.SectorDepositoId))
                ModelState.AddModelError(nameof(modelo.SectorDepositoId), "La ubicación seleccionada no es válida.");

            ValidarFechas(modelo.FechaElaboracion, modelo.FechaVencimiento,
                pallet.Producto?.Categoria?.ControlaVencimiento == true, esIngresoNuevo: false);

            string? remitoNuevo = null;
            if (ModelState.IsValid && modelo.RemitoArchivo is { Length: > 0 })
            {
                var (ruta, error) = await _archivos.GuardarRemitoAsync(modelo.RemitoArchivo, _usuarioActual.EmpresaId!.Value);
                if (error != null) ModelState.AddModelError(nameof(modelo.RemitoArchivo), error);
                remitoNuevo = ruta;
            }

            if (!ModelState.IsValid)
            {
                await CompletarEdicionAsync(modelo, pallet);
                ViewData["Pie"] = await _auditoria.ArmarPieAsync(pallet);
                return View(modelo);
            }

            pallet.SectorDepositoId = modelo.SectorDepositoId!.Value;
            pallet.CodigoLote = modelo.CodigoLote;
            pallet.FechaElaboracion = modelo.FechaElaboracion?.Date;
            pallet.FechaVencimiento = modelo.FechaVencimiento?.Date;

            // Remito: el nuevo reemplaza al anterior; o se quita el existente
            var remitoAnterior = pallet.RemitoRuta;
            if (remitoNuevo != null)
            {
                pallet.RemitoRuta = remitoNuevo;
                pallet.RemitoNombreOriginal = NombreRemito(modelo.RemitoArchivo!.FileName);
            }
            else if (modelo.QuitarRemito)
            {
                pallet.RemitoRuta = null;
                pallet.RemitoNombreOriginal = null;
            }

            try
            {
                await _contexto.SaveChangesAsync();
            }
            catch
            {
                _archivos.EliminarRemito(remitoNuevo);
                throw;
            }

            // El archivo viejo se borra recién cuando el cambio quedó guardado
            if (pallet.RemitoRuta != remitoAnterior) _archivos.EliminarRemito(remitoAnterior);

            TempData["Exito"] = $"El pallet '{pallet.CodigoPallet}' se actualizó correctamente.";
            return RedirectToAction(nameof(Detalle), new { id });
        }

        // Descarga del remito en PDF. El filtro global del DbContext hace que solo se encuentren
        // pallets de la empresa (y sucursal) del usuario: otra cuenta recibe "no encontrado".
        [HttpGet]
        public async Task<IActionResult> Remito(int id)
        {
            var pallet = await _contexto.Pallets.AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new { p.RemitoRuta, p.RemitoNombreOriginal, p.CodigoPallet })
                .FirstOrDefaultAsync();
            if (pallet == null) return NotFound();

            var fisica = _archivos.RutaFisicaRemito(pallet.RemitoRuta);
            if (fisica == null) return NotFound();

            var nombre = pallet.RemitoNombreOriginal ?? $"remito-{pallet.CodigoPallet}.pdf";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers.ContentDisposition = "inline; filename*=UTF-8''" + Uri.EscapeDataString(nombre);
            return PhysicalFile(fisica, "application/pdf");
        }

        // Nombre original del archivo, sin ruta ni caracteres de control, de hasta 150 caracteres
        private static string NombreRemito(string nombreArchivo)
        {
            var nombre = new string(Path.GetFileName(nombreArchivo).Where(c => !char.IsControl(c)).ToArray()).Trim();
            if (nombre.Length == 0) nombre = "remito.pdf";
            return nombre.Length > 150 ? nombre[^150..] : nombre;
        }

        // Reglas de negocio del ingreso. Los filtros globales aseguran que producto y
        // sector se validen solo contra datos de la propia empresa y sucursal.
        private async Task ValidarIngresoAsync(PalletFormViewModel m)
        {
            if (!string.IsNullOrEmpty(m.CodigoPallet) &&
                await _contexto.Pallets.AnyAsync(p => p.CodigoPallet == m.CodigoPallet))
                ModelState.AddModelError(nameof(m.CodigoPallet), "Ya existe un pallet con ese código en su sucursal.");

            Producto? producto = null;
            if (m.ProductoId.HasValue)
            {
                producto = await _contexto.Productos.Include(p => p.Categoria)
                    .FirstOrDefaultAsync(p => p.Id == m.ProductoId && p.Activo);
                if (producto == null)
                    ModelState.AddModelError(nameof(m.ProductoId), "El producto seleccionado no está disponible.");
            }

            if (m.SectorDepositoId.HasValue &&
                !await _contexto.SectoresDeposito.AnyAsync(s => s.Id == m.SectorDepositoId))
                ModelState.AddModelError(nameof(m.SectorDepositoId), "La ubicación seleccionada no es válida.");

            ValidarFechas(m.FechaElaboracion, m.FechaVencimiento,
                producto?.Categoria?.ControlaVencimiento == true, esIngresoNuevo: true);
        }

        // Mismos nombres de propiedad en ambos ViewModels, por eso se usan literales
        private void ValidarFechas(DateTime? elaboracion, DateTime? vencimiento, bool exigeVencimiento, bool esIngresoNuevo)
        {
            var hoy = FormatoFecha.HoyArgentina;

            if (exigeVencimiento && !vencimiento.HasValue)
                ModelState.AddModelError("FechaVencimiento", "La categoría de este producto exige fecha de vencimiento.");

            if (elaboracion.HasValue && elaboracion.Value.Date > hoy)
                ModelState.AddModelError("FechaElaboracion", "La fecha de elaboración no puede ser futura.");

            if (elaboracion.HasValue && vencimiento.HasValue && vencimiento.Value.Date < elaboracion.Value.Date)
                ModelState.AddModelError("FechaVencimiento", "El vencimiento no puede ser anterior a la elaboración.");

            // No se ingresa mercadería que ya está vencida
            if (esIngresoNuevo && vencimiento.HasValue && vencimiento.Value.Date < hoy)
                ModelState.AddModelError("FechaVencimiento", "No se puede ingresar un pallet con fecha de vencimiento pasada.");
        }

        private async Task CargarListasAsync(PalletFormViewModel modelo)
        {
            var productos = await _contexto.Productos.AsNoTracking()
                .Where(p => p.Activo)
                .OrderBy(p => p.Nombre)
                .Select(p => new { p.Id, p.CodigoSku, p.Nombre, Controla = p.Categoria!.ControlaVencimiento })
                .ToListAsync();

            modelo.Productos = productos
                .Select(p => new SelectListItem($"{p.CodigoSku} - {p.Nombre}", p.Id.ToString())).ToList();
            modelo.ProductosConVencimiento = productos.ToDictionary(p => p.Id, p => p.Controla);
            modelo.Sectores = await ListaSectoresAsync();
        }

        private async Task CompletarEdicionAsync(PalletEdicionViewModel modelo, Pallet pallet)
        {
            modelo.CodigoPallet = pallet.CodigoPallet;
            modelo.ProductoNombre = pallet.Producto?.Nombre ?? string.Empty;
            modelo.CantidadActual = pallet.CantidadActual;
            modelo.ExigeVencimiento = pallet.Producto?.Categoria?.ControlaVencimiento == true;
            modelo.RemitoNombre = pallet.RemitoRuta == null ? null : pallet.RemitoNombreOriginal ?? "Remito";
            modelo.Sectores = await ListaSectoresAsync();
        }

        private async Task<List<SelectListItem>> ListaSectoresAsync()
        {
            var sectores = await _contexto.SectoresDeposito.AsNoTracking()
                .OrderBy(s => s.NombreSector).ThenBy(s => s.Pasillo).ThenBy(s => s.Estante).ToListAsync();

            return sectores.Select(s =>
            {
                var texto = s.NombreSector;
                if (!string.IsNullOrEmpty(s.Pasillo)) texto += $" - Pasillo {s.Pasillo}";
                if (!string.IsNullOrEmpty(s.Estante)) texto += $" - Estante {s.Estante}";
                return new SelectListItem(texto, s.Id.ToString());
            }).ToList();
        }
    }
}