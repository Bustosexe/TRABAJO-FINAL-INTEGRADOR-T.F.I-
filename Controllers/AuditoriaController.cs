using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockSuper.Models;
using StockSuper.Services;

namespace StockSuper.Controllers
{
    // Auditoría para la Dirección de la empresa: los cambios sobre su catálogo, pallets y
    // demás datos, y los accesos de sus cuentas. Solo de su propia empresa. Los usuarios de
    // sucursal no tienen acceso.
    [Authorize(Roles = Roles.DirectorEmpresa)]
    public class AuditoriaController : Controller
    {
        private readonly ServicioConsultaAuditoria _consulta;
        private readonly IUsuarioActual _usuarioActual;

        public AuditoriaController(ServicioConsultaAuditoria consulta, IUsuarioActual usuarioActual)
        {
            _consulta = consulta;
            _usuarioActual = usuarioActual;
        }

        public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta, string? buscar, int? ambito) =>
            View(await _consulta.ConsultarAsync(
                false, _usuarioActual.EmpresaId, desde, hasta, buscar, ambito, null));
    }
}
