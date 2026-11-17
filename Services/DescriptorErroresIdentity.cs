using Microsoft.AspNetCore.Identity;

namespace StockSuper.Services
{
    // Traduce al español los errores que devuelve Identity al crear cuentas
    // o cambiar contraseñas (política de contraseñas, duplicados, etc.).
    public class DescriptorErroresIdentity : IdentityErrorDescriber
    {
        public override IdentityError DefaultError() =>
            new() { Code = nameof(DefaultError), Description = "Ocurrió un error inesperado." };

        public override IdentityError ConcurrencyFailure() =>
            new() { Code = nameof(ConcurrencyFailure), Description = "El registro fue modificado por otro usuario. Actualice e intente nuevamente." };

        public override IdentityError PasswordMismatch() =>
            new() { Code = nameof(PasswordMismatch), Description = "La contraseña actual es incorrecta." };

        public override IdentityError InvalidToken() =>
            new() { Code = nameof(InvalidToken), Description = "El código de seguridad no es válido." };

        public override IdentityError InvalidUserName(string? userName) =>
            new() { Code = nameof(InvalidUserName), Description = $"El nombre de usuario '{userName}' no es válido." };

        public override IdentityError InvalidEmail(string? email) =>
            new() { Code = nameof(InvalidEmail), Description = $"El correo electrónico '{email}' no es válido." };

        public override IdentityError DuplicateUserName(string userName) =>
            new() { Code = nameof(DuplicateUserName), Description = $"El usuario '{userName}' ya está registrado." };

        public override IdentityError DuplicateEmail(string email) =>
            new() { Code = nameof(DuplicateEmail), Description = $"El correo '{email}' ya está registrado en otra cuenta." };

        public override IdentityError InvalidRoleName(string? role) =>
            new() { Code = nameof(InvalidRoleName), Description = $"El nombre de rol '{role}' no es válido." };

        public override IdentityError DuplicateRoleName(string role) =>
            new() { Code = nameof(DuplicateRoleName), Description = $"El rol '{role}' ya existe." };

        public override IdentityError UserAlreadyHasPassword() =>
            new() { Code = nameof(UserAlreadyHasPassword), Description = "La cuenta ya tiene una contraseña asignada." };

        public override IdentityError UserAlreadyInRole(string role) =>
            new() { Code = nameof(UserAlreadyInRole), Description = $"La cuenta ya tiene el rol '{role}'." };

        public override IdentityError UserNotInRole(string role) =>
            new() { Code = nameof(UserNotInRole), Description = $"La cuenta no tiene el rol '{role}'." };

        public override IdentityError PasswordTooShort(int length) =>
            new() { Code = nameof(PasswordTooShort), Description = $"La contraseña debe tener al menos {length} caracteres." };

        public override IdentityError PasswordRequiresNonAlphanumeric() =>
            new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "La contraseña debe incluir al menos un símbolo." };

        public override IdentityError PasswordRequiresDigit() =>
            new() { Code = nameof(PasswordRequiresDigit), Description = "La contraseña debe incluir al menos un número." };

        public override IdentityError PasswordRequiresLower() =>
            new() { Code = nameof(PasswordRequiresLower), Description = "La contraseña debe incluir al menos una letra minúscula." };

        public override IdentityError PasswordRequiresUpper() =>
            new() { Code = nameof(PasswordRequiresUpper), Description = "La contraseña debe incluir al menos una letra mayúscula." };

        public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
            new() { Code = nameof(PasswordRequiresUniqueChars), Description = $"La contraseña debe tener al menos {uniqueChars} caracteres distintos." };
    }
}