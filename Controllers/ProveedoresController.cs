using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockSuper.Data;
using StockSuper.Models;
using StockSuper.Services;

namespace StockSuper.Controllers
{
    // ABM de proveedores de la empresa. No se eliminan: se dan de baja (Activo = false)
    // para conservar el historial de los productos y pallets que abastecieron.
    [Authorize(Roles = Roles.DirectorEmpresa)]
    public class ProveedoresController : Controller
    {
        private readonly ApplicationDbContext _contexto;
        private readonly ServicioAuditoria _auditoria;

        public ProveedoresController(ApplicationDbContext contexto, ServicioAuditoria auditoria)
        {
            _contexto = contexto;
            _auditoria = auditoria;
        }

        // Listado con búsqueda por razón social o CUIT, y filtro por estado
        public async Task<IActionResult> Index(string? buscar, string? estado)
        {
            var consulta = _contexto.Proveedores.Include(p => p.Productos).AsNoTracking();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var texto = buscar.Trim();
                consulta = consulta.Where(p => p.RazonSocial.Contains(texto) || p.Cuit.Contains(texto));
            }

            if (estado == "activos") consulta = consulta.Where(p => p.Activo);
            else if (estado == "inactivos") consulta = consulta.Where(p => !p.Activo);

            var lista = await consulta.OrderBy(p => p.RazonSocial).ToListAsync();

            ViewData["Buscar"] = buscar;
            ViewData["Estado"] = estado;
            ViewData["Usuarios"] = await _auditoria.NombresAsync(
                lista.SelectMany(p => new[] { p.UsuarioCreacionId, p.UsuarioModificacionId }));
            return View(lista);
        }

        [HttpGet]
        public IActionResult Crear() => View("Formulario", new Proveedor());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(
            [Bind("RazonSocial,Cuit,Telefono,Email,Direccion")] Proveedor proveedor)
        {
            proveedor.RazonSocial = proveedor.RazonSocial?.Trim() ?? string.Empty;
            proveedor.Cuit = proveedor.Cuit?.Trim() ?? string.Empty;
            proveedor.Activo = true;

            if (await _contexto.Proveedores.AnyAsync(p => p.Cuit == proveedor.Cuit))
                ModelState.AddModelError(nameof(proveedor.Cuit), "Ya existe un proveedor con ese CUIT.");

            if (!ModelState.IsValid) return View("Formulario", proveedor);

            _contexto.Proveedores.Add(proveedor);
            await _contexto.SaveChangesAsync();

            TempData["Exito"] = $"El proveedor '{proveedor.RazonSocial}' se registró correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var proveedor = await _contexto.Proveedores.FirstOrDefaultAsync(p => p.Id == id);
            if (proveedor == null) return NotFound();

            ViewData["Pie"] = await _auditoria.ArmarPieAsync(proveedor);
            return View("Formulario", proveedor);
        }

        // El nombre C# es distinto para evitar el conflicto con el GET, pero
        // ActionName mantiene la ruta "Editar" para el formulario
        [HttpPost, ValidateAntiForgeryToken, ActionName("Editar")]
        public async Task<IActionResult> EditarConfirmado(int id)
        {
            var proveedor = await _contexto.Proveedores.FirstOrDefaultAsync(p => p.Id == id);
            if (proveedor == null) return NotFound();

            var actualizado = await TryUpdateModelAsync(proveedor, "",
                p => p.RazonSocial, p => p.Cuit, p => p.Telefono, p => p.Email, p => p.Direccion);

            proveedor.RazonSocial = proveedor.RazonSocial?.Trim() ?? string.Empty;
            proveedor.Cuit = proveedor.Cuit?.Trim() ?? string.Empty;

            if (await _contexto.Proveedores.AnyAsync(p => p.Cuit == proveedor.Cuit && p.Id != id))
                ModelState.AddModelError(nameof(proveedor.Cuit), "Ya existe otro proveedor con ese CUIT.");

            if (!actualizado || !ModelState.IsValid)
            {
                ViewData["Pie"] = await _auditoria.ArmarPieAsync(proveedor);
                return View("Formulario", proveedor);
            }

            await _contexto.SaveChangesAsync();
            TempData["Exito"] = $"El proveedor '{proveedor.RazonSocial}' se actualizó correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // Baja lógica: alterna entre activo e inactivo
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var proveedor = await _contexto.Proveedores.FirstOrDefaultAsync(p => p.Id == id);
            if (proveedor == null) return NotFound();

            proveedor.Activo = !proveedor.Activo;
            await _contexto.SaveChangesAsync();

            TempData["Exito"] = proveedor.Activo
                ? $"El proveedor '{proveedor.RazonSocial}' fue reactivado."
                : $"El proveedor '{proveedor.RazonSocial}' fue dado de baja.";
            return RedirectToAction(nameof(Index));
        }
    }
}