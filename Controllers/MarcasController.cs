using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockSuper.Data;
using StockSuper.Models;
using StockSuper.Services;

namespace StockSuper.Controllers
{
    // ABM de marcas comerciales. Solo la Dirección de empresa gestiona el catálogo.
    [Authorize(Roles = Roles.DirectorEmpresa)]
    public class MarcasController : Controller
    {
        private readonly ApplicationDbContext _contexto;
        private readonly ServicioAuditoria _auditoria;

        public MarcasController(ApplicationDbContext contexto, ServicioAuditoria auditoria)
        {
            _contexto = contexto;
            _auditoria = auditoria;
        }

        public async Task<IActionResult> Index(string? buscar)
        {
            var consulta = _contexto.Marcas.Include(m => m.Productos).AsNoTracking();

            if (!string.IsNullOrWhiteSpace(buscar))
                consulta = consulta.Where(m => m.Nombre.Contains(buscar.Trim()));

            var lista = await consulta.OrderBy(m => m.Nombre).ToListAsync();

            ViewData["Buscar"] = buscar;
            ViewData["Usuarios"] = await _auditoria.NombresAsync(
                lista.SelectMany(m => new[] { m.UsuarioCreacionId, m.UsuarioModificacionId }));
            return View(lista);
        }

        [HttpGet]
        public IActionResult Crear() => View("Formulario", new Marca());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear([Bind("Nombre")] Marca marca)
        {
            marca.Nombre = marca.Nombre?.Trim() ?? string.Empty;

            if (await _contexto.Marcas.AnyAsync(m => m.Nombre == marca.Nombre))
                ModelState.AddModelError(nameof(marca.Nombre), "Ya existe una marca con ese nombre.");

            if (!ModelState.IsValid) return View("Formulario", marca);

            _contexto.Marcas.Add(marca);
            await _contexto.SaveChangesAsync();

            TempData["Exito"] = $"La marca '{marca.Nombre}' se registró correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var marca = await _contexto.Marcas.FirstOrDefaultAsync(m => m.Id == id);
            if (marca == null) return NotFound();

            ViewData["Pie"] = await _auditoria.ArmarPieAsync(marca);
            return View("Formulario", marca);
        }

        // El nombre C# es distinto para evitar el conflicto con el GET, pero
        // ActionName mantiene la ruta "Editar" para el formulario
        [HttpPost, ValidateAntiForgeryToken, ActionName("Editar")]
        public async Task<IActionResult> EditarConfirmado(int id)
        {
            var marca = await _contexto.Marcas.FirstOrDefaultAsync(m => m.Id == id);
            if (marca == null) return NotFound();

            var actualizado = await TryUpdateModelAsync(marca, "", m => m.Nombre);
            marca.Nombre = marca.Nombre?.Trim() ?? string.Empty;

            if (await _contexto.Marcas.AnyAsync(m => m.Nombre == marca.Nombre && m.Id != id))
                ModelState.AddModelError(nameof(marca.Nombre), "Ya existe otra marca con ese nombre.");

            if (!actualizado || !ModelState.IsValid)
            {
                ViewData["Pie"] = await _auditoria.ArmarPieAsync(marca);
                return View("Formulario", marca);
            }

            await _contexto.SaveChangesAsync();
            TempData["Exito"] = $"La marca '{marca.Nombre}' se actualizó correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var marca = await _contexto.Marcas.FirstOrDefaultAsync(m => m.Id == id);
            if (marca == null) return NotFound();

            if (await _contexto.Productos.AnyAsync(p => p.MarcaId == id))
            {
                TempData["Error"] = "No se puede eliminar: la marca tiene productos asociados.";
                return RedirectToAction(nameof(Index));
            }

            _contexto.Marcas.Remove(marca);
            await _contexto.SaveChangesAsync();

            TempData["Exito"] = $"La marca '{marca.Nombre}' se eliminó correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}