namespace StockSuper.Models.Enums
{
    // Tipo de evento de la bitácora de auditoría. Define quién puede verlo:
    //  - Plataforma: altas y cambios de empresas, sucursales y cuentas (los ve el administrador).
    //  - Acceso: ingresos, intentos fallidos, cierres de sesión y cambios de contraseña
    //    (los ve el administrador y, de su empresa, la Dirección).
    //  - Negocio: cambios sobre el catálogo, los pallets y demás datos de la empresa
    //    (los ve solo la Dirección de esa empresa: el administrador nunca los ve).
    public enum AmbitoAuditoria
    {
        Plataforma = 1,
        Acceso = 2,
        Negocio = 3
    }
}
