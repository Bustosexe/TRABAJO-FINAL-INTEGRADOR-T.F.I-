using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockSuper.Models;
using StockSuper.Services;

namespace StockSuper.Controllers
{
    // Localidades de una provincia para el desplegable de los formularios de empresas y
    // sucursales. Solo lo usa el administrador de la plataforma, que es quien las carga.
    // Devuelve JSON; el navegador nunca consulta directamente al servicio externo.
    [Authorize(Roles = Roles.AdministradorPlataforma)]
    public class UbicacionesController : Controller
    {
        private readonly ServicioUbicaciones _ubicaciones;

        public UbicacionesController(ServicioUbicaciones ubicaciones) => _ubicaciones = ubicaciones;

        [HttpGet]
        public async Task<IActionResult> Localidades(string? provincia, CancellationToken cancelacion)
        {
            var lista = await _ubicaciones.LocalidadesAsync(provincia, cancelacion);
            return lista == null
                ? Json(new { ok = false })
                : Json(new { ok = true, localidades = lista });
        }
    }
}
