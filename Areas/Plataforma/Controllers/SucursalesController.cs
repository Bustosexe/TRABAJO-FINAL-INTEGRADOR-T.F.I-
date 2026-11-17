using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StockSuper.Data;
using StockSuper.Models;
using StockSuper.Models.Enums;
using StockSuper.Models.ViewModels;
using StockSuper.Services;

namespace StockSuper.Areas.Plataforma.Controllers
{
    // ABM de sucursales de las empresas clientes. Nunca se eliminan: se cierran, y conservan
    // todo su historial. Toda consulta usa IgnoreQueryFilters porque el administrador no
    // pertenece a ninguna empresa y, por diseño, los filtros globales le ocultan los datos
    // de negocio. En esta área solo se leen datos de la sucursal, nunca de su stock.
    [Area("Plataforma")]
    [Authorize(Roles = Roles.AdministradorPlataforma)]
    public class SucursalesController : Controller
    {
        private readonly ApplicationDbContext _contexto;
        private readonly ServicioAuditoria _auditoria;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ServicioUbicaciones _ubicaciones;

        public SucursalesController(
            ApplicationDbContext contexto, ServicioAuditoria auditoria,
            UserManager<ApplicationUser> userManager, ServicioUbicaciones ubicaciones)
        {
            _contexto = contexto;
            _auditoria = auditoria;
            _userManager = userManager;
            _ubicaciones = ubicaciones;
        }

        public async Task<IActionResult> Index(int? empresaId, string? buscar, string? estado)
        {
            var consulta = _contexto.Sucursales.IgnoreQueryFilters().AsNoTracking()
                .Include(s => s.Empresa).AsQueryable();

            if (empresaId.HasValue) consulta = consulta.Where(s => s.EmpresaId == empresaId);

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var texto = buscar.Trim();
                consulta = consulta.Where(s => s.Nombre.Contains(texto) || s.Codigo.Contains(texto)
                    || s.Localidad.Contains(texto) || s.Direccion.Contains(texto));
            }

            if (estado == "activas") consulta = consulta.Where(s => s.Estado == EstadoSucursal.Activa);
            else if (estado == "cerradas") consulta = consulta.Where(s => s.Estado == EstadoSucursal.Cerrada);

            var lista = await consulta
                .OrderBy(s => s.Empresa!.RazonSocial).ThenByDescending(s => s.EsCasaCentral).ThenBy(s => s.Codigo)
                .ToListAsync();

            ViewData["Empresas"] = new SelectList(
                await _contexto.Empresas.AsNoTracking().OrderBy(e => e.RazonSocial).ToListAsync(),
                "Id", "RazonSocial", empresaId);
            ViewData["Buscar"] = buscar;
            ViewData["Estado"] = estado;
            ViewData["EmpresaId"] = empresaId;
            ViewData["Usuarios"] = await _auditoria.NombresAsync(
                lista.SelectMany(s => new[] { s.UsuarioCreacionId, s.UsuarioModificacionId }));
            return View(lista);
        }

        [HttpGet]
        public async Task<IActionResult> Crear(int? empresaId)
        {
            var modelo = new SucursalFormViewModel { EmpresaId = empresaId };
            await CargarEmpresasAsync(modelo);
            return View("Formulario", modelo);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(SucursalFormViewModel modelo)
        {
            Normalizar(modelo);
            await ValidarUbicacionAsync(modelo);

            if (!modelo.EmpresaId.HasValue)
            {
                ModelState.AddModelError(nameof(modelo.EmpresaId), "Seleccione una empresa.");
            }
            else if (!await _contexto.Empresas.AnyAsync(e => e.Id == modelo.EmpresaId && e.Activa))
            {
                ModelState.AddModelError(nameof(modelo.EmpresaId), "La empresa seleccionada no está disponible.");
            }
            else if (await _contexto.Sucursales.IgnoreQueryFilters()
                .AnyAsync(s => s.EmpresaId == modelo.EmpresaId && s.Codigo == modelo.Codigo))
            {
                ModelState.AddModelError(nameof(modelo.Codigo), "Esa empresa ya tiene una sucursal con ese código.");
            }

            if (!ModelState.IsValid)
            {
                await CargarEmpresasAsync(modelo);
                return View("Formulario", modelo);
            }

            var sucursal = new Sucursal
            {
                // El administrador no tiene empresa propia, por eso asigna EmpresaId de forma explícita
                EmpresaId = modelo.EmpresaId!.Value,
                Codigo = modelo.Codigo,
                Nombre = modelo.Nombre,
                EsCasaCentral = false,
                Direccion = modelo.Direccion,
                Localidad = modelo.Localidad,
                Provincia = modelo.Provincia,
                Estado = EstadoSucursal.Activa
            };

            _contexto.Sucursales.Add(sucursal);
            await _contexto.SaveChangesAsync();

            TempData["Exito"] = $"La sucursal '{sucursal.Nombre}' se registró correctamente.";
            return RedirectToAction(nameof(Index), new { empresaId = sucursal.EmpresaId });
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var sucursal = await _contexto.Sucursales.IgnoreQueryFilters().AsNoTracking()
                .Include(s => s.Empresa).FirstOrDefaultAsync(s => s.Id == id);
            if (sucursal == null) return NotFound();

            ViewData["Pie"] = await _auditoria.ArmarPieAsync(sucursal);
            return View("Formulario", Mapear(sucursal));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, SucursalFormViewModel modelo)
        {
            var sucursal = await _contexto.Sucursales.IgnoreQueryFilters()
                .Include(s => s.Empresa).FirstOrDefaultAsync(s => s.Id == id);
            if (sucursal == null) return NotFound();

            modelo.Id = id;
            Normalizar(modelo);
            await ValidarUbicacionAsync(modelo);

            if (await _contexto.Sucursales.IgnoreQueryFilters()
                .AnyAsync(s => s.EmpresaId == sucursal.EmpresaId && s.Codigo == modelo.Codigo && s.Id != id))
                ModelState.AddModelError(nameof(modelo.Codigo), "Esa empresa ya tiene otra sucursal con ese código.");

            if (!ModelState.IsValid)
            {
                modelo.EmpresaNombre = sucursal.Empresa?.RazonSocial ?? string.Empty;
                modelo.EsCasaCentral = sucursal.EsCasaCentral;
                ViewData["Pie"] = await _auditoria.ArmarPieAsync(sucursal);
                return View("Formulario", modelo);
            }

            // La empresa y la condición de casa central no se modifican desde el formulario
            sucursal.Codigo = modelo.Codigo;
            sucursal.Nombre = modelo.Nombre;
            sucursal.Direccion = modelo.Direccion;
            sucursal.Localidad = modelo.Localidad;
            sucursal.Provincia = modelo.Provincia;
           
            await _contexto.SaveChangesAsync();
            TempData["Exito"] = $"La sucursal '{sucursal.Nombre}' se actualizó correctamente.";
            return RedirectToAction(nameof(Index), new { empresaId = sucursal.EmpresaId });
        }

        // Cerrar o reabrir. No existe la acción de eliminar: una sucursal conserva su historial.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var sucursal = await _contexto.Sucursales.IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == id);
            if (sucursal == null) return NotFound();

            if (sucursal.EsCasaCentral && sucursal.Estado == EstadoSucursal.Activa)
            {
                TempData["Error"] = "La casa central no se puede cerrar. Si la empresa deja de operar, suspenda la empresa.";
                return RedirectToAction(nameof(Index), new { empresaId = sucursal.EmpresaId });
            }

            var cerrando = sucursal.Estado == EstadoSucursal.Activa;
            sucursal.Estado = cerrando ? EstadoSucursal.Cerrada : EstadoSucursal.Activa;
            await _contexto.SaveChangesAsync();

            if (cerrando)
            {
                // Las cuentas de la sucursal se desactivan y sus sesiones abiertas se cierran
                var cuentas = await _userManager.Users
                    .Where(u => u.SucursalId == id && u.Activo).ToListAsync();
                foreach (var cuenta in cuentas)
                {
                    cuenta.Activo = false;
                    await _userManager.UpdateAsync(cuenta);
                    await _userManager.UpdateSecurityStampAsync(cuenta);
                }
            }

            TempData["Exito"] = cerrando
                ? $"La sucursal '{sucursal.Nombre}' fue cerrada. Su historial se conserva y sus cuentas fueron desactivadas."
                : $"La sucursal '{sucursal.Nombre}' fue reabierta. Sus cuentas se reactivan desde la sección Cuentas.";
            return RedirectToAction(nameof(Index), new { empresaId = sucursal.EmpresaId });
        }

        // Provincia y localidad deben figurar en el listado oficial (se guardan con su escritura oficial)
        private async Task ValidarUbicacionAsync(SucursalFormViewModel modelo)
        {
            var ubicacion = await _ubicaciones.ValidarAsync(modelo.Provincia, modelo.Localidad);
            modelo.Provincia = ubicacion.Provincia;
            modelo.Localidad = ubicacion.Localidad;
            ubicacion.AplicarErrores(ModelState, nameof(modelo.Provincia), nameof(modelo.Localidad));
        }

        private static void Normalizar(SucursalFormViewModel m)
        {
            m.Codigo = m.Codigo?.Trim().ToUpperInvariant() ?? string.Empty;
            m.Nombre = m.Nombre?.Trim() ?? string.Empty;
            m.Direccion = m.Direccion?.Trim() ?? string.Empty;
            m.Localidad = m.Localidad?.Trim() ?? string.Empty;
            m.Provincia = m.Provincia?.Trim() ?? string.Empty;
        }

        private static SucursalFormViewModel Mapear(Sucursal s) => new()
        {
            Id = s.Id,
            EmpresaId = s.EmpresaId,
            EmpresaNombre = s.Empresa?.RazonSocial ?? string.Empty,
            EsCasaCentral = s.EsCasaCentral,
            Codigo = s.Codigo,
            Nombre = s.Nombre,
            Direccion = s.Direccion,
            Localidad = s.Localidad,
            Provincia = s.Provincia
        };

        // Solo se ofrecen empresas con contrato activo
        private async Task CargarEmpresasAsync(SucursalFormViewModel modelo)
        {
            modelo.Empresas = await _contexto.Empresas.AsNoTracking().Where(e => e.Activa)
                .OrderBy(e => e.RazonSocial)
                .Select(e => new SelectListItem(e.RazonSocial, e.Id.ToString())).ToListAsync();
        }
    }
}