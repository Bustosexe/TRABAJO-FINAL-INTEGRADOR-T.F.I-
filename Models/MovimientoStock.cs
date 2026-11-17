using System.ComponentModel.DataAnnotations;
using StockSuper.Models.Enums;

namespace StockSuper.Models
{
    // Registro de cada entrada o salida de unidades de un pallet. Es la base
    // de los reportes: stock inicial + entradas - salidas = stock final.
    // Los movimientos nunca se editan ni se borran, solo se agregan.
    public class MovimientoStock : SucursalEntity
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Seleccione un pallet.")]
        [Display(Name = "Pallet")]
        public int PalletId { get; set; }

        [Display(Name = "Fecha del movimiento")]
        public DateTime FechaMovimiento { get; set; }

        [Required(ErrorMessage = "Seleccione el tipo de movimiento.")]
        [Display(Name = "Tipo de movimiento")]
        public TipoMovimiento TipoMovimiento { get; set; }

        [Required(ErrorMessage = "Seleccione el motivo.")]
        [Display(Name = "Motivo")]
        public MotivoMovimiento Motivo { get; set; }

        [Required(ErrorMessage = "La cantidad es obligatoria.")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero.")]
        [Display(Name = "Cantidad")]
        public int Cantidad { get; set; }

        [StringLength(250, ErrorMessage = "La observación no puede superar los 250 caracteres.")]
        [Display(Name = "Observación")]
        public string? Observacion { get; set; }

        // Cuenta que ejecutó la operación (FK a AspNetUsers). Se asigna en el servidor.
        public string UsuarioId { get; set; } = string.Empty;

        public Pallet? Pallet { get; set; }
        public ApplicationUser? Usuario { get; set; }
    }
}