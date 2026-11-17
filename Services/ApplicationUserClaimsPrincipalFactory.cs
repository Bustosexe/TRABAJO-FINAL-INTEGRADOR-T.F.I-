using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using StockSuper.Models;

namespace StockSuper.Services
{
    // Al iniciar sesión, agrega a la cookie la empresa y la sucursal asignadas
    // a la cuenta. Así el usuario no puede elegir ni cambiar su sucursal:
    // queda fija desde que el administrador crea la cuenta.
    public class ApplicationUserClaimsPrincipalFactory
        : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
    {
        public ApplicationUserClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<IdentityOptions> options)
            : base(userManager, roleManager, options) { }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            var identidad = await base.GenerateClaimsAsync(user);

            if (user.EmpresaId.HasValue)
                identidad.AddClaim(new Claim(NombresClaim.EmpresaId, user.EmpresaId.Value.ToString()));

            if (user.SucursalId.HasValue)
                identidad.AddClaim(new Claim(NombresClaim.SucursalId, user.SucursalId.Value.ToString()));

            identidad.AddClaim(new Claim(NombresClaim.NombreCuenta, user.NombreCuenta));

            // Si la contraseña es temporal, el middleware obliga a cambiarla
            if (user.DebeCambiarPassword)
                identidad.AddClaim(new Claim(NombresClaim.DebeCambiarPassword, "true"));

            return identidad;
        }
    }
}