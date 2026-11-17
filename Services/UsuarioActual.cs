using System.Security.Claims;
using StockSuper.Models;

namespace StockSuper.Services
{
    // Lee empresa y sucursal desde los claims de la sesión. Como el HttpContext
    // puede no existir (migraciones, tareas en segundo plano), todo es nulable.
    public class UsuarioActual : IUsuarioActual
    {
        private readonly IHttpContextAccessor _http;

        public UsuarioActual(IHttpContextAccessor http) => _http = http;

        private ClaimsPrincipal? Principal => _http.HttpContext?.User;

        public string? UsuarioId => Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

        public int? EmpresaId => LeerEntero(NombresClaim.EmpresaId);

        public int? SucursalId => LeerEntero(NombresClaim.SucursalId);

        public string? NombreCuenta =>
            Principal?.FindFirstValue(NombresClaim.NombreCuenta) ?? Principal?.Identity?.Name;

        public string? DireccionIp => _http.HttpContext?.Connection.RemoteIpAddress?.ToString();

        private int? LeerEntero(string claim)
        {
            var valor = Principal?.FindFirstValue(claim);
            return int.TryParse(valor, out var numero) ? numero : null;
        }
    }
}