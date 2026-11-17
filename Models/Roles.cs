namespace StockSuper.Models
{
    // Nombres de los roles del sistema. Se usan en [Authorize(Roles = ...)]
    // y en la carga inicial de datos, para no repetir cadenas de texto.
    public static class Roles
    {
        public const string AdministradorPlataforma = "AdministradorPlataforma";
        public const string DirectorEmpresa = "DirectorEmpresa";
        public const string UsuarioSucursal = "UsuarioSucursal";
    }

    // Nombres de los claims personalizados guardados en la cookie de sesión
    public static class NombresClaim
    {
        public const string EmpresaId = "EmpresaId";
        public const string SucursalId = "SucursalId";
        public const string NombreCuenta = "NombreCuenta";
        // Indica que la cuenta tiene contraseña temporal y debe cambiarla
        public const string DebeCambiarPassword = "DebeCambiarPassword";
    }
}