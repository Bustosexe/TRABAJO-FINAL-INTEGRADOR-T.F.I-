using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StockSuper.Data;
using StockSuper.Models.Enums;
using StockSuper.Models.ViewModels;

namespace StockSuper.Services
{
    // Consulta de la bitácora de auditoría. Es el único lugar donde se decide qué eventos
    // puede ver cada rol, para que las dos pantallas (administrador y Dirección) no
    // puedan divergir:
    //  - Administrador de plataforma: eventos de Plataforma y de Acceso de todas las empresas.
    //    Nunca eventos de Negocio: es información de cada cliente.
    //  - Dirección: eventos de Negocio y de Acceso, solo de su propia empresa.
    public class ServicioConsultaAuditoria
    {
        private const int MaximoFilas = 200;

        private readonly ApplicationDbContext _contexto;

        public ServicioConsultaAuditoria(ApplicationDbContext contexto) => _contexto = contexto;

        public async Task<AuditoriaViewModel> ConsultarAsync(
            bool esPlataforma, int? empresaPropia, DateTime? desde, DateTime? hasta,
            string? buscar, int? ambito, int? empresaId)
        {
            var modelo = new AuditoriaViewModel
            {
                Titulo = esPlataforma ? "Auditoría de la plataforma" : "Auditoría de la empresa",
                Subtitulo = esPlataforma
                    ? "Altas y cambios de empresas, sucursales y cuentas, y accesos al sistema. No incluye operaciones de stock: son información de cada cliente."
                    : "Cambios sobre el catálogo, los pallets y demás datos de la empresa, y accesos de sus cuentas.",
                Desde = desde,
                Hasta = hasta,
                Buscar = buscar,
                Ambito = ambito,
                MostrarEmpresa = esPlataforma,
                Maximo = MaximoFilas,
                Ambitos = esPlataforma
                    ? new List<OpcionAmbito>
                    {
                        new() { Valor = (int)AmbitoAuditoria.Plataforma, Texto = EtiquetasAuditoria.Ambito(AmbitoAuditoria.Plataforma) },
                        new() { Valor = (int)AmbitoAuditoria.Acceso, Texto = EtiquetasAuditoria.Ambito(AmbitoAuditoria.Acceso) }
                    }
                    : new List<OpcionAmbito>
                    {
                        new() { Valor = (int)AmbitoAuditoria.Negocio, Texto = EtiquetasAuditoria.Ambito(AmbitoAuditoria.Negocio) },
                        new() { Valor = (int)AmbitoAuditoria.Acceso, Texto = EtiquetasAuditoria.Ambito(AmbitoAuditoria.Acceso) }
                    }
            };

            // Una cuenta de empresa sin empresa asignada no debe ver nada
            if (!esPlataforma && !empresaPropia.HasValue) return modelo;

            var consulta = _contexto.RegistrosAuditoria.AsNoTracking().AsQueryable();

            if (esPlataforma)
            {
                consulta = consulta.Where(r => r.Ambito != AmbitoAuditoria.Negocio);
                if (empresaId.HasValue)
                {
                    modelo.EmpresaId = empresaId;
                    consulta = consulta.Where(r => r.EmpresaId == empresaId);
                }

                modelo.Empresas = new SelectList(
                    await _contexto.Empresas.AsNoTracking().OrderBy(e => e.RazonSocial).ToListAsync(),
                    "Id", "RazonSocial", empresaId);
            }
            else
            {
                consulta = consulta.Where(r => r.EmpresaId == empresaPropia && r.Ambito != AmbitoAuditoria.Plataforma);
            }

            // Solo se aceptan los tipos que corresponden al rol
            if (ambito.HasValue && modelo.Ambitos.Any(o => o.Valor == ambito.Value))
                consulta = consulta.Where(r => r.Ambito == (AmbitoAuditoria)ambito.Value);

            // Las fechas del filtro son de Argentina (UTC-3) y en la base están en UTC
            if (desde.HasValue)
            {
                var inicioUtc = desde.Value.Date.AddHours(3);
                consulta = consulta.Where(r => r.FechaUtc >= inicioUtc);
            }
            if (hasta.HasValue)
            {
                var finUtc = hasta.Value.Date.AddDays(1).AddHours(3);
                consulta = consulta.Where(r => r.FechaUtc < finUtc);
            }

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var texto = buscar.Trim();
                consulta = consulta.Where(r => r.UsuarioNombre.Contains(texto)
                    || r.Accion.Contains(texto)
                    || (r.Entidad != null && r.Entidad.Contains(texto))
                    || (r.Detalle != null && r.Detalle.Contains(texto)));
            }

            modelo.Total = await consulta.CountAsync();

            var registros = await consulta
                .OrderByDescending(r => r.FechaUtc).ThenByDescending(r => r.Id)
                .Take(MaximoFilas)
                .ToListAsync();

            // Nombres de empresa y sucursal, en dos consultas para todas las filas
            var idsEmpresa = registros.Where(r => r.EmpresaId.HasValue).Select(r => r.EmpresaId!.Value).Distinct().ToList();
            var idsSucursal = registros.Where(r => r.SucursalId.HasValue).Select(r => r.SucursalId!.Value).Distinct().ToList();

            var empresas = await _contexto.Empresas.AsNoTracking()
                .Where(e => idsEmpresa.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, e => e.RazonSocial);

            // IgnoreQueryFilters: los Id ya salen de filas permitidas para este rol
            var sucursales = await _contexto.Sucursales.IgnoreQueryFilters().AsNoTracking()
                .Where(s => idsSucursal.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Nombre);

            modelo.Filas = registros.Select(r => new AuditoriaFilaViewModel
            {
                FechaUtc = r.FechaUtc,
                Ambito = r.Ambito,
                Accion = r.Accion,
                Entidad = r.Entidad,
                Detalle = r.Detalle,
                Cuenta = r.UsuarioNombre,
                Empresa = r.EmpresaId.HasValue && empresas.TryGetValue(r.EmpresaId.Value, out var e) ? e : null,
                Sucursal = r.SucursalId.HasValue && sucursales.TryGetValue(r.SucursalId.Value, out var s) ? s : null,
                Direccion = r.Direccion
            }).ToList();

            return modelo;
        }
    }
}
