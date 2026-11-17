using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockSuper.Data;
using StockSuper.Models;
using StockSuper.Services;

namespace StockSuper.Controllers
{
    // Sectores, pasillos y estantes del depósito de UNA sucursal. Solo el usuario de
    // la sucursal los gestiona; el filtro global garantiza que nunca vea los de otra.
    [Authorize(Roles = Roles.UsuarioSucursal)]
    public class SectoresController : Controller
    {
        private readonly ApplicationDbContext _contexto;
        private readonly ServicioAuditoria _auditoria;

        public SectoresController(ApplicationDbContext contexto, ServicioAuditoria auditoria)
        {
            _contexto = contexto;
            _auditoria = auditoria;
        }

        public async Task<IActionResult> Index(string? buscar)
        {
            var consulta = _contexto.SectoresDeposito.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var texto = buscar.Trim();
                consulta = consulta.Where(s => s.NombreSector.Contains(texto)
                    || (s.Pasillo != null && s.Pasillo.Contains(texto))
                    || (s.Estante != null && s.Estante.Contains(texto)));
            }

            var lista = await consulta.OrderBy(s => s.NombreSector).ThenBy(s => s.Pasillo).ThenBy(s => s.Estante).ToListAsync();

            // Cantidad de pallets por sector, en una sola consulta agrupada
            ViewData["Conteo"] = await _contexto.Pallets.AsNoTracking()
                .GroupBy(p => p.SectorDepositoId)
                .Select(g => new { g.Key, Total = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Total);

            ViewData["Buscar"] = buscar;
            ViewData["Usuarios"] = await _auditoria.NombresAsync(
                lista.SelectMany(s => new[] { s.UsuarioCreacionId, s.UsuarioModificacionId }));
            return View(lista);
        }

        [HttpGet]
        public IActionResult Crear() => View("Formulario", new SectorDeposito());

        // [Bind]: la sucursal, la empresa y la auditoría nunca vienen del navegador
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear([Bind("NombreSector,Pasillo,Estante")] SectorDeposito sector)
        {
            Normalizar(sector);

            if (await Duplicado(sector, null))
                ModelState.AddModelError(string.Empty, "Ya existe un sector con ese nombre, pasillo y estante.");

            if (!ModelState.IsValid) return View("Formulario", sector);

            _contexto.SectoresDeposito.Add(sector);
            await _contexto.SaveChangesAsync();

            TempData["Exito"] = $"El sector '{sector.NombreSector}' se registró correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var sector = await _contexto.SectoresDeposito.FirstOrDefaultAsync(s => s.Id == id);
            if (sector == null) return NotFound();

            ViewData["Pie"] = await _auditoria.ArmarPieAsync(sector);
            return View("Formulario", sector);
        }

        [HttpPost, ValidateAntiForgeryToken, ActionName("Editar")]
        public async Task<IActionResult> EditarConfirmado(int id)
        {
            var sector = await _contexto.SectoresDeposito.FirstOrDefaultAsync(s => s.Id == id);
            if (sector == null) return NotFound();

            var actualizado = await TryUpdateModelAsync(sector, "",
                s => s.NombreSector, s => s.Pasillo, s => s.Estante);
            Normalizar(sector);

            if (await Duplicado(sector, id))
                ModelState.AddModelError(string.Empty, "Ya existe otro sector con ese nombre, pasillo y estante.");

            if (!actualizado || !ModelState.IsValid)
            {
                ViewData["Pie"] = await _auditoria.ArmarPieAsync(sector);
                return View("Formulario", sector);
            }

            await _contexto.SaveChangesAsync();
            TempData["Exito"] = $"El sector '{sector.NombreSector}' se actualizó correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // Un sector con pallets no se elimina: primero hay que reubicar su mercadería
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var sector = await _contexto.SectoresDeposito.FirstOrDefaultAsync(s => s.Id == id);
            if (sector == null) return NotFound();

            if (await _contexto.Pallets.AnyAsync(p => p.SectorDepositoId == id))
            {
                TempData["Error"] = "No se puede eliminar: el sector tiene pallets asignados. Reubique la mercadería primero.";
                return RedirectToAction(nameof(Index));
            }

            _contexto.SectoresDeposito.Remove(sector);
            await _contexto.SaveChangesAsync();

            TempData["Exito"] = $"El sector '{sector.NombreSector}' se eliminó correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private static void Normalizar(SectorDeposito s)
        {
            s.NombreSector = s.NombreSector?.Trim() ?? string.Empty;
            s.Pasillo = string.IsNullOrWhiteSpace(s.Pasillo) ? null : s.Pasillo.Trim();
            s.Estante = string.IsNullOrWhiteSpace(s.Estante) ? null : s.Estante.Trim();
        }

        private Task<bool> Duplicado(SectorDeposito s, int? idActual) =>
            _contexto.SectoresDeposito.AnyAsync(x =>
                x.NombreSector == s.NombreSector && x.Pasillo == s.Pasillo
                && x.Estante == s.Estante && x.Id != idActual);
    }
}