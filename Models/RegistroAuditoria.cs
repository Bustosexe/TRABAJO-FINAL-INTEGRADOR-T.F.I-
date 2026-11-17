using System.ComponentModel.DataAnnotations;
using StockSuper.Models.Enums;

namespace StockSuper.Models
{
    // Bitácora de auditoría: una fila por evento (quién hizo qué y cuándo). Es de solo
    // agregado: nunca se edita ni se elimina. El nombre de la cuenta se guarda como texto
    // (no como clave foránea) para que el registro se conserve aunque la cuenta cambie.
    public class RegistroAuditoria
    {
        public long Id { get; set; }

        // Se guarda en UTC; en pantalla se convierte a hora de Argentina
        public DateTime FechaUtc { get; set; }

        public AmbitoAuditoria Ambito { get; set; }

        // Ej.: "Alta", "Modificación", "Ingreso", "Cuenta creada"
        [Required, StringLength(100)]
        public string Accion { get; set; } = string.Empty;

        // Sobre qué se actuó (ej.: "Producto", "Cuenta") y su identificador
        [StringLength(60)]
        public string? Entidad { get; set; }

        [StringLength(40)]
        public string? EntidadId { get; set; }

        [StringLength(500)]
        public string? Detalle { get; set; }

        // Cuenta que realizó la operación
        [StringLength(450)]
        public string? UsuarioId { get; set; }

        [Required, StringLength(150)]
        public string UsuarioNombre { get; set; } = string.Empty;

        // Empresa y sucursal a las que pertenece el evento; definen quién puede verlo
        public int? EmpresaId { get; set; }
        public int? SucursalId { get; set; }

        // Dirección IP de origen (útil en los eventos de acceso)
        [StringLength(45)]
        public string? Direccion { get; set; }
    }
}
