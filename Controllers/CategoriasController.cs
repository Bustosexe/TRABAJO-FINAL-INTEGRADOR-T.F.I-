using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockSuper.Data;
using StockSuper.Models;
using StockSuper.Services;

namespace StockSuper.Controllers
{
    // ABM de categorías (rubros). Solo la Dirección de empresa gestiona el catálogo.
    // El filtro global del DbContext limita todas las consultas a la empresa del usuario.
    [Authorize(Roles = Roles.DirectorEmpresa)]
    public class CategoriasController : Controller
    {
        private readonly ApplicationDbContext _contexto;
        private readonly ServicioAuditoria _auditoria;

        public CategoriasController(ApplicationDbContext contexto, ServicioAuditoria auditoria)
        {
            _contexto = contexto;
            _auditoria = auditoria;
        }

        // Listado con búsqueda por nombre
        public async Task<IActionResult> Index(string? buscar)
        {
            var consulta = _contexto.Categorias.Include(c => c.Productos).AsNoTracking();

            if (!string.IsNullOrWhiteSpace(buscar))
                consulta = consulta.Where(c => c.Nombre.Contains(buscar.Trim()));

            var lista = await consulta.OrderBy(c => c.Nombre).ToListAsync();

            ViewData["Buscar"] = buscar;
            ViewData["Usuarios"] = await _auditoria.NombresAsync(
                lista.SelectMany(c => new[] { c.UsuarioCreacionId, c.UsuarioModificacionId }));
            return View(lista);
        }

        [HttpGet]
        public IActionResult Crear() => View("Formulario", new Categoria());

        // [Bind] limita los campos aceptados del formulario (evita sobre-posteo):
        // la empresa y los datos de auditoría nunca vienen del navegador.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(
            [Bind("Nombre,Descripcion,ControlaVencimiento")] Categoria categoria)
        {
            categoria.Nombre = categoria.Nombre?.Trim() ?? string.Empty;

            if (await _contexto.Categorias.AnyAsync(c => c.Nombre == categoria.Nombre))
                ModelState.AddModelError(nameof(categoria.Nombre), "Ya existe una categoría con ese nombre.");

            if (!ModelState.IsValid) return View("Formulario", categoria);

            _contexto.Categorias.Add(categoria);
            await _contexto.SaveChangesAsync();

            TempData["Exito"] = $"La categoría '{categoria.Nombre}' se registró correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var categoria = await _contexto.Categorias.FirstOrDefaultAsync(c => c.Id == id);
            if (categoria == null) return NotFound();

            ViewData["Pie"] = await _auditoria.ArmarPieAsync(categoria);
            return View("Formulario", categoria);
        }

        // El nombre C# es distinto para evitar el conflicto con el GET, pero
        // ActionName mantiene la ruta "Editar" para el formulario
        [HttpPost, ValidateAntiForgeryToken, ActionName("Editar")]
        public async Task<IActionResult> EditarConfirmado(int id)
        {
            var categoria = await _contexto.Categorias.FirstOrDefaultAsync(c => c.Id == id);
            if (categoria == null) return NotFound();

            // Solo se actualizan los campos permitidos; la auditoría la completa el DbContext
            var actualizado = await TryUpdateModelAsync(categoria, "",
                c => c.Nombre, c => c.Descripcion, c => c.ControlaVencimiento);

            categoria.Nombre = categoria.Nombre?.Trim() ?? string.Empty;

            if (await _contexto.Categorias.AnyAsync(c => c.Nombre == categoria.Nombre && c.Id != id))
                ModelState.AddModelError(nameof(categoria.Nombre), "Ya existe otra categoría con ese nombre.");

            if (!actualizado || !ModelState.IsValid)
            {
                ViewData["Pie"] = await _auditoria.ArmarPieAsync(categoria);
                return View("Formulario", categoria);
            }

            await _contexto.SaveChangesAsync();
            TempData["Exito"] = $"La categoría '{categoria.Nombre}' se actualizó correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // Solo se elimina si ningún producto la usa; la base también lo impide (Restrict)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var categoria = await _contexto.Categorias.FirstOrDefaultAsync(c => c.Id == id);
            if (categoria == null) return NotFound();

            if (await _contexto.Productos.AnyAsync(p => p.CategoriaId == id))
            {
                TempData["Error"] = "No se puede eliminar: la categoría tiene productos asociados.";
                return RedirectToAction(nameof(Index));
            }

            _contexto.Categorias.Remove(categoria);
            await _contexto.SaveChangesAsync();

            TempData["Exito"] = $"La categoría '{categoria.Nombre}' se eliminó correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}