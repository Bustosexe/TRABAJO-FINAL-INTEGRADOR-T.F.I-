using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockSuper.Data;
using StockSuper.Models;
using StockSuper.Models.Enums;
using StockSuper.Models.ViewModels;
using StockSuper.Services;

namespace StockSuper.Areas.Plataforma.Controllers
{
    // ABM de empresas clientes. Solo el administrador de la plataforma. Las empresas no
    // se eliminan: se suspenden (contrato vencido, baja del servicio) y se reactivan.
    // Esta área nunca consulta stock: el inventario es información de cada cliente.
    [Area("Plataforma")]
    [Authorize(Roles = Roles.AdministradorPlataforma)]
    public class EmpresasController : Controller
    {
        private readonly ApplicationDbContext _contexto;
        private readonly ServicioAuditoria _auditoria;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ServicioUbicaciones _ubicaciones;

        public EmpresasController(
            ApplicationDbContext contexto, ServicioAuditoria auditoria,
            UserManager<ApplicationUser> userManager, ServicioUbicaciones ubicaciones)
        {
            _contexto = contexto;
            _auditoria = auditoria;
            _userManager = userManager;
            _ubicaciones = ubicaciones;
        }

        public async Task<IActionResult> Index(string? buscar, string? estado)
        {
            var consulta = _contexto.Empresas.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var texto = buscar.Trim();
                consulta = consulta.Where(e => e.RazonSocial.Contains(texto) || e.Cuit.Contains(texto));
            }

            if (estado == "activas") consulta = consulta.Where(e => e.Activa);
            else if (estado == "suspendidas") consulta = consulta.Where(e => !e.Activa);

            var lista = await consulta.OrderBy(e => e.RazonSocial).ToListAsync();

            // IgnoreQueryFilters: el administrador no tiene empresa propia, por lo que los
            // filtros globales le ocultarían las sucursales. Solo se cuentan, no se leen datos de stock.
            ViewData["Sucursales"] = await _contexto.Sucursales.IgnoreQueryFilters().AsNoTracking()
                .GroupBy(s => s.EmpresaId)
                .Select(g => new { g.Key, Total = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Total);

            ViewData["Cuentas"] = await _contexto.Users.AsNoTracking()
                .Where(u => u.EmpresaId != null)
                .GroupBy(u => u.EmpresaId!.Value)
                .Select(g => new { g.Key, Total = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Total);

            ViewData["Buscar"] = buscar;
            ViewData["Estado"] = estado;
            ViewData["Usuarios"] = await _auditoria.NombresAsync(
                lista.SelectMany(e => new[] { e.UsuarioCreacionId, e.UsuarioModificacionId }));
            return View(lista);
        }

        [HttpGet]
        public IActionResult Crear() => View(new EmpresaCrearViewModel { FechaContrato = FormatoFecha.HoyArgentina });

        // Empresa y casa central se guardan juntas en una sola transacción
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(EmpresaCrearViewModel modelo)
        {
            modelo.RazonSocial = modelo.RazonSocial?.Trim() ?? string.Empty;
            modelo.Cuit = modelo.Cuit?.Trim() ?? string.Empty;
            modelo.CentralCodigo = modelo.CentralCodigo?.Trim().ToUpperInvariant() ?? string.Empty;
            modelo.CentralNombre = modelo.CentralNombre?.Trim() ?? string.Empty;

            if (await _contexto.Empresas.AnyAsync(e => e.Cuit == modelo.Cuit))
                ModelState.AddModelError(nameof(modelo.Cuit), "Ya existe una empresa con ese CUIT.");

            // Provincia y localidad deben figurar en el listado oficial (se guardan con su escritura oficial)
            var ubicacion = await _ubicaciones.ValidarAsync(modelo.CentralProvincia, modelo.CentralLocalidad);
            modelo.CentralProvincia = ubicacion.Provincia;
            modelo.CentralLocalidad = ubicacion.Localidad;
            ubicacion.AplicarErrores(ModelState, nameof(modelo.CentralProvincia), nameof(modelo.CentralLocalidad));

            if (!ModelState.IsValid) return View(modelo);

            var empresa = new Empresa
            {
                RazonSocial = modelo.RazonSocial,
                Cuit = modelo.Cuit,
                Email = modelo.Email?.Trim(),
                Telefono = modelo.Telefono?.Trim(),
                FechaContrato = modelo.FechaContrato?.Date,
                Activa = true
            };

            // El EmpresaId de la sucursal lo completa EF al guardar ambas juntas
            empresa.Sucursales.Add(new Sucursal
            {
                Codigo = modelo.CentralCodigo,
                Nombre = modelo.CentralNombre,
                EsCasaCentral = true,
                Direccion = modelo.CentralDireccion.Trim(),
                Localidad = modelo.CentralLocalidad.Trim(),
                Provincia = modelo.CentralProvincia.Trim(),
                Estado = EstadoSucursal.Activa
            });

            _contexto.Empresas.Add(empresa);
            await _contexto.SaveChangesAsync();

            TempData["Exito"] = $"La empresa '{empresa.RazonSocial}' se registró con su casa central. El siguiente paso es crear sus cuentas de acceso.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var empresa = await _contexto.Empresas.FirstOrDefaultAsync(e => e.Id == id);
            if (empresa == null) return NotFound();

            ViewData["Pie"] = await _auditoria.ArmarPieAsync(empresa);
            return View(empresa);
        }

        [HttpPost, ValidateAntiForgeryToken, ActionName("Editar")]
        public async Task<IActionResult> EditarConfirmado(int id)
        {
            var empresa = await _contexto.Empresas.FirstOrDefaultAsync(e => e.Id == id);
            if (empresa == null) return NotFound();

            // Solo se actualizan los campos permitidos; el estado y la auditoría no vienen del formulario
            var actualizado = await TryUpdateModelAsync(empresa, "",
                e => e.RazonSocial, e => e.Cuit, e => e.Email, e => e.Telefono, e => e.FechaContrato);

            empresa.RazonSocial = empresa.RazonSocial?.Trim() ?? string.Empty;
            empresa.Cuit = empresa.Cuit?.Trim() ?? string.Empty;

            if (await _contexto.Empresas.AnyAsync(e => e.Cuit == empresa.Cuit && e.Id != id))
                ModelState.AddModelError(nameof(empresa.Cuit), "Ya existe otra empresa con ese CUIT.");

            if (!actualizado || !ModelState.IsValid)
            {
                ViewData["Pie"] = await _auditoria.ArmarPieAsync(empresa);
                return View(empresa);
            }

            await _contexto.SaveChangesAsync();
            TempData["Exito"] = $"La empresa '{empresa.RazonSocial}' se actualizó correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // Suspender o reactivar. Al suspender se invalidan las sesiones abiertas de sus cuentas.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var empresa = await _contexto.Empresas.FirstOrDefaultAsync(e => e.Id == id);
            if (empresa == null) return NotFound();

            empresa.Activa = !empresa.Activa;
            await _contexto.SaveChangesAsync();

            if (!empresa.Activa)
            {
                // Cambiar el sello de seguridad cierra las sesiones en la próxima revalidación (1 minuto)
                var cuentas = await _userManager.Users.Where(u => u.EmpresaId == id).ToListAsync();
                foreach (var cuenta in cuentas)
                    await _userManager.UpdateSecurityStampAsync(cuenta);
            }

            TempData["Exito"] = empresa.Activa
                ? $"La empresa '{empresa.RazonSocial}' fue reactivada. Sus cuentas pueden volver a ingresar."
                : $"La empresa '{empresa.RazonSocial}' fue suspendida. Sus cuentas ya no pueden ingresar.";
            return RedirectToAction(nameof(Index));
        }
    }
}