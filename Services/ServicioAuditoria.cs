using Microsoft.EntityFrameworkCore;
using StockSuper.Data;
using StockSuper.Models;
using StockSuper.Models.ViewModels;

namespace StockSuper.Services
{
    // Traduce los ids de usuario guardados en los campos de auditoría al nombre
    // de la cuenta que realizó cada operación (ej.: "Supermercados Demo - Sucursal 202").
    public class ServicioAuditoria
    {
        private readonly ApplicationDbContext _contexto;

        public ServicioAuditoria(ApplicationDbContext contexto) => _contexto = contexto;

        // Diccionario id de usuario -> nombre de cuenta, en una sola consulta
        public async Task<Dictionary<string, string>> NombresAsync(IEnumerable<string?> ids)
        {
            var lista = ids.Where(i => i != null).Select(i => i!).Distinct().ToList();
            return await _contexto.Users
                .Where(u => lista.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.NombreCuenta);
        }

        // Pie de auditoría para las pantallas de edición
        public async Task<PieAuditoriaViewModel> ArmarPieAsync(AuditableEntity entidad)
        {
            var nombres = await NombresAsync(new[] { entidad.UsuarioCreacionId, entidad.UsuarioModificacionId });

            return new PieAuditoriaViewModel
            {
                CreadoPor = entidad.UsuarioCreacionId != null
                    && nombres.TryGetValue(entidad.UsuarioCreacionId, out var creador) ? creador : "Sistema",
                FechaCreacion = entidad.FechaCreacion,
                ModificadoPor = entidad.UsuarioModificacionId != null
                    && nombres.TryGetValue(entidad.UsuarioModificacionId, out var modificador) ? modificador : null,
                FechaModificacion = entidad.FechaModificacion
            };
        }

        // Texto para los listados: "Cuenta - dd/MM/yyyy HH:mm" de la última acción sobre el registro
        public static string UltimaAccion(AuditableEntity entidad, Dictionary<string, string> nombres)
        {
            var usuarioId = entidad.UsuarioModificacionId ?? entidad.UsuarioCreacionId;
            var fecha = (entidad.FechaModificacion ?? entidad.FechaCreacion).AHoraArgentina();
            var nombre = usuarioId != null && nombres.TryGetValue(usuarioId, out var n) ? n : "Sistema";
            return $"{nombre} - {fecha:dd/MM/yyyy HH:mm}";
        }
    }
}