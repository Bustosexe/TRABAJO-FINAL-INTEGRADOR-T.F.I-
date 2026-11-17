using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StockSuper.Models;
using StockSuper.Models.Enums;

namespace StockSuper.Data
{
    // Carga inicial: roles, empresa de demostración y cuentas de prueba.
    // Las contraseñas se leen de la configuración (User Secrets en desarrollo,
    // variables de entorno en producción). Nunca van escritas en el código.
    public static class DbInicializador
    {
        public static async Task InicializarAsync(IServiceProvider servicios)
        {
            var contexto = servicios.GetRequiredService<ApplicationDbContext>();
            var roleManager = servicios.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = servicios.GetRequiredService<UserManager<ApplicationUser>>();
            var config = servicios.GetRequiredService<IConfiguration>();

            // Aplica migraciones pendientes (útil en el despliegue)
            await contexto.Database.MigrateAsync();

            // 1. Roles del sistema
            foreach (var rol in new[] { Roles.AdministradorPlataforma, Roles.DirectorEmpresa, Roles.UsuarioSucursal })
            {
                if (!await roleManager.RoleExistsAsync(rol))
                    await roleManager.CreateAsync(new IdentityRole(rol));
            }

            // 2. Cuenta del administrador de la plataforma (sin empresa ni sucursal)
            await CrearCuentaAsync(userManager,
                email: config["SeedUsuarios:AdminEmail"] ?? "admin@stocksuper.com",
                password: config["SeedUsuarios:AdminPassword"],
                nombreCuenta: "Administrador de la plataforma",
                rol: Roles.AdministradorPlataforma,
                empresaId: null, sucursalId: null);

            // 3. Empresa de demostración con casa central y una sucursal.
            // IgnoreQueryFilters: durante el arranque no hay usuario logueado.
            var empresa = await contexto.Empresas.FirstOrDefaultAsync(e => e.Cuit == "30700000001");
            if (empresa == null)
            {
                empresa = new Empresa
                {
                    RazonSocial = "Supermercados Demo S.A.",
                    Cuit = "30700000001",
                    Email = "contacto@demo.com",
                    FechaContrato = DateTime.Today,
                    Activa = true
                };
                contexto.Empresas.Add(empresa);
                await contexto.SaveChangesAsync();
            }

            var centralExiste = await contexto.Sucursales.IgnoreQueryFilters()
                .AnyAsync(s => s.EmpresaId == empresa.Id && s.Codigo == "001");
            if (!centralExiste)
            {
                contexto.Sucursales.AddRange(
                    new Sucursal
                    {
                        EmpresaId = empresa.Id,
                        Codigo = "001",
                        Nombre = "Casa Central",
                        EsCasaCentral = true,
                        Direccion = "Av. San Martín 100",
                        Localidad = "Santa Rosa",
                        Provincia = "La Pampa",
                        Latitud = -36.620300m,
                        Longitud = -64.290600m,
                        Estado = EstadoSucursal.Activa
                    },
                    new Sucursal
                    {
                        EmpresaId = empresa.Id,
                        Codigo = "002",
                        Nombre = "Sucursal 002",
                        EsCasaCentral = false,
                        Direccion = "Calle Pellegrini 850",
                        Localidad = "Santa Rosa",
                        Provincia = "La Pampa",
                        Latitud = -36.612000m,
                        Longitud = -64.280000m,
                        Estado = EstadoSucursal.Activa
                    });
                await contexto.SaveChangesAsync();
            }

            var sucursal002 = await contexto.Sucursales.IgnoreQueryFilters()
                .FirstAsync(s => s.EmpresaId == empresa.Id && s.Codigo == "002");

            // 4. Cuenta del director de la empresa (todas las sucursales)
            await CrearCuentaAsync(userManager,
                email: config["SeedUsuarios:DirectorEmail"] ?? "director@demo.com",
                password: config["SeedUsuarios:DirectorPassword"],
                nombreCuenta: "Supermercados Demo - Dirección",
                rol: Roles.DirectorEmpresa,
                empresaId: empresa.Id, sucursalId: null);

            // 5. Cuenta de una sucursal (solo ve y opera su propia sucursal)
            await CrearCuentaAsync(userManager,
                email: config["SeedUsuarios:SucursalEmail"] ?? "sucursal002@demo.com",
                password: config["SeedUsuarios:SucursalPassword"],
                nombreCuenta: "Supermercados Demo - Sucursal 002",
                rol: Roles.UsuarioSucursal,
                empresaId: empresa.Id, sucursalId: sucursal002.Id);
        }

        private static async Task CrearCuentaAsync(
            UserManager<ApplicationUser> userManager, string email, string? password,
            string nombreCuenta, string rol, int? empresaId, int? sucursalId)
        {
            if (string.IsNullOrWhiteSpace(password)) return; // Sin clave configurada, no se crea

            if (await userManager.FindByEmailAsync(email) != null) return;

            var usuario = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                NombreCuenta = nombreCuenta,
                EmpresaId = empresaId,
                SucursalId = sucursalId,
                DebeCambiarPassword = false, // Cuentas de prueba para la corrección
                Activo = true
            };

            var resultado = await userManager.CreateAsync(usuario, password);
            if (resultado.Succeeded)
                await userManager.AddToRoleAsync(usuario, rol);
        }
    }
}