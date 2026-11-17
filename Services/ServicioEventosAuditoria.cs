using StockSuper.Data;
using StockSuper.Models;
using StockSuper.Models.Enums;

namespace StockSuper.Services
{
    // Registra en la bitácora los eventos que no son un simple cambio de datos: ingresos y
    // cierres de sesión, intentos fallidos, altas y bajas de cuentas, correos enviados.
    // Los cambios de datos de negocio los registra automáticamente el DbContext.
    // Si el registro falla, se informa en el log del servidor y la operación principal
    // continúa: auditar no debe impedir que alguien pueda ingresar o trabajar.
    public class ServicioEventosAuditoria
    {
        private readonly ApplicationDbContext _contexto;
        private readonly IUsuarioActual _usuarioActual;
        private readonly ILogger<ServicioEventosAuditoria> _logger;

        public ServicioEventosAuditoria(
            ApplicationDbContext contexto, IUsuarioActual usuarioActual, ILogger<ServicioEventosAuditoria> logger)
        {
            _contexto = contexto;
            _usuarioActual = usuarioActual;
            _logger = logger;
        }

        // Sin usuarioId se toma la cuenta que tiene la sesión abierta, con su empresa y
        // sucursal. Con usuarioId (ingresos, o el administrador actuando sobre otra
        // cuenta) los datos de empresa y sucursal se indican de forma explícita.
        public async Task RegistrarAsync(
            AmbitoAuditoria ambito, string accion, string? detalle = null,
            string? entidad = null, string? entidadId = null,
            int? empresaId = null, int? sucursalId = null,
            string? usuarioId = null, string? usuarioNombre = null)
        {
            var actuaLaSesion = usuarioId == null;

            var registro = new RegistroAuditoria
            {
                FechaUtc = DateTime.UtcNow,
                Ambito = ambito,
                Accion = Recortar(accion, 100)!,
                Entidad = Recortar(entidad, 60),
                EntidadId = Recortar(entidadId, 40),
                Detalle = Recortar(detalle, 500),
                UsuarioId = usuarioId ?? _usuarioActual.UsuarioId,
                UsuarioNombre = Recortar(usuarioNombre ?? _usuarioActual.NombreCuenta ?? "Sistema", 150)!,
                EmpresaId = empresaId ?? (actuaLaSesion ? _usuarioActual.EmpresaId : null),
                SucursalId = sucursalId ?? (actuaLaSesion ? _usuarioActual.SucursalId : null),
                Direccion = Recortar(_usuarioActual.DireccionIp, 45)
            };

            try
            {
                _contexto.RegistrosAuditoria.Add(registro);
                await _contexto.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Se descarta solo este registro para que no se reintente en el próximo guardado
                _contexto.Entry(registro).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                _logger.LogError(ex, "No se pudo registrar el evento de auditoría '{Accion}'", accion);
            }
        }

        private static string? Recortar(string? texto, int maximo) =>
            texto == null ? null : texto.Length > maximo ? texto[..maximo] : texto;
    }
}
