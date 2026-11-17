using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockSuper.Models;
using StockSuper.Services;

namespace StockSuper.Areas.Plataforma.Controllers
{
    // Auditoría para el administrador de la plataforma: altas y cambios de empresas,
    // sucursales y cuentas, y accesos de todas las cuentas. Nunca muestra operaciones
    // de stock (eso lo decide ServicioConsultaAuditoria para este rol).
    [Area("Plataforma")]
    [Authorize(Roles = Roles.AdministradorPlataforma)]
    public class AuditoriaController : Controller
    {
        private readonly ServicioConsultaAuditoria _consulta;

        public AuditoriaController(ServicioConsultaAuditoria consulta) => _consulta = consulta;

        public async Task<IActionResult> Index(
            DateTime? desde, DateTime? hasta, string? buscar, int? ambito, int? empresaId) =>
            View(await _consulta.ConsultarAsync(true, null, desde, hasta, buscar, ambito, empresaId));
    }
}
