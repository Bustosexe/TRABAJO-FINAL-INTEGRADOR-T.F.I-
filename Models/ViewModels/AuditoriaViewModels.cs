using Microsoft.AspNetCore.Mvc.Rendering;
using StockSuper.Models.Enums;

namespace StockSuper.Models.ViewModels
{
    // Fila de la pantalla de auditoría
    public class AuditoriaFilaViewModel
    {
        public DateTime FechaUtc { get; set; }
        public AmbitoAuditoria Ambito { get; set; }
        public string Accion { get; set; } = string.Empty;
        public string? Entidad { get; set; }
        public string? Detalle { get; set; }
        public string Cuenta { get; set; } = string.Empty;
        public string? Empresa { get; set; }
        public string? Sucursal { get; set; }
        public string? Direccion { get; set; }
    }

    public class OpcionAmbito
    {
        public int Valor { get; set; }
        public string Texto { get; set; } = string.Empty;
    }

    // Pantalla de auditoría. La usan el administrador (eventos de plataforma y de acceso,
    // de todas las empresas) y la Dirección (operaciones y accesos de su empresa).
    public class AuditoriaViewModel
    {
        public string Titulo { get; set; } = string.Empty;
        public string Subtitulo { get; set; } = string.Empty;

        public List<AuditoriaFilaViewModel> Filas { get; set; } = new();
        public int Total { get; set; }
        public int Maximo { get; set; }

        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public string? Buscar { get; set; }
        public int? Ambito { get; set; }
        public int? EmpresaId { get; set; }

        // El filtro y la columna de empresa solo tienen sentido para el administrador
        public bool MostrarEmpresa { get; set; }
        public IEnumerable<SelectListItem> Empresas { get; set; } = new List<SelectListItem>();
        public List<OpcionAmbito> Ambitos { get; set; } = new();
    }
}
