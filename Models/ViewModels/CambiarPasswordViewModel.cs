using System.ComponentModel.DataAnnotations;

namespace StockSuper.Models.ViewModels
{
    // Datos del formulario de cambio de contraseña
    public class CambiarPasswordViewModel
    {
        [Required(ErrorMessage = "Ingrese la contraseña actual.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña actual")]
        public string PasswordActual { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingrese la nueva contraseña.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva contraseña")]
        public string PasswordNueva { get; set; } = string.Empty;

        [Required(ErrorMessage = "Repita la nueva contraseña.")]
        [DataType(DataType.Password)]
        [Compare(nameof(PasswordNueva), ErrorMessage = "La confirmación no coincide con la nueva contraseña.")]
        [Display(Name = "Confirmar nueva contraseña")]
        public string ConfirmarPassword { get; set; } = string.Empty;
    }
}