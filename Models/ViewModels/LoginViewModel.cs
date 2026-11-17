using System.ComponentModel.DataAnnotations;

namespace StockSuper.Models.ViewModels
{
    // Datos del formulario de ingreso
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Ingrese el correo electrónico de la cuenta.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingrese la contraseña.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        // Sesión persistente: el equipo de la sucursal no reingresa credenciales a diario
        [Display(Name = "Mantener la sesión iniciada en este equipo")]
        public bool Recordarme { get; set; } = true;
    }
}