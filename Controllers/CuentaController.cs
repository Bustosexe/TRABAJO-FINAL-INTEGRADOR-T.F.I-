using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockSuper.Data;
using StockSuper.Models;
using StockSuper.Models.Enums;
using StockSuper.Models.ViewModels;
using StockSuper.Services;

namespace StockSuper.Controllers
{
    // Ingreso, cierre de sesión y cambio de contraseña. No hay registro público:
    // las cuentas las crea el administrador de la plataforma.
    public class CuentaController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _contexto;
        private readonly ServicioEventosAuditoria _eventos;

        public CuentaController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext contexto,
            ServicioEventosAuditoria eventos)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _contexto = contexto;
            _eventos = eventos;
        }

        // Evento de acceso de una cuenta que todavía no tiene la sesión abierta (ingreso).
        // Si el correo no existe, se registra el texto ingresado como nombre.
        private Task RegistrarAccesoAsync(
            string accion, string? detalle, ApplicationUser? usuario, string? correoIngresado = null) =>
            _eventos.RegistrarAsync(AmbitoAuditoria.Acceso, accion, detalle, "Cuenta",
                usuario?.Id, usuario?.EmpresaId, usuario?.SucursalId,
                usuario?.Id, usuario?.NombreCuenta ?? correoIngresado);

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel modelo, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid) return View(modelo);

            const string errorGenerico = "Correo o contraseña incorrectos.";

            var usuario = await _userManager.FindByEmailAsync(modelo.Email);
            if (usuario == null)
            {
                await RegistrarAccesoAsync("Ingreso fallido", "Correo no registrado", null, modelo.Email);
                ModelState.AddModelError(string.Empty, errorGenerico);
                return View(modelo);
            }

            // Verifica la contraseña y cuenta los intentos fallidos (bloqueo temporal)
            var resultado = await _signInManager.CheckPasswordSignInAsync(
                usuario, modelo.Password, lockoutOnFailure: true);

            if (resultado.IsLockedOut)
            {
                await RegistrarAccesoAsync("Cuenta bloqueada", "Bloqueo temporal por intentos fallidos", usuario);
                ModelState.AddModelError(string.Empty,
                    "La cuenta fue bloqueada temporalmente por intentos fallidos. Intente nuevamente en unos minutos.");
                return View(modelo);
            }

            if (!resultado.Succeeded)
            {
                await RegistrarAccesoAsync("Ingreso fallido", "Contraseña incorrecta", usuario);
                ModelState.AddModelError(string.Empty, errorGenerico);
                return View(modelo);
            }

            // Cuenta dada de baja por el administrador
            if (!usuario.Activo)
            {
                await RegistrarAccesoAsync("Ingreso rechazado", "Cuenta desactivada", usuario);
                ModelState.AddModelError(string.Empty, "La cuenta se encuentra desactivada. Contacte al administrador.");
                return View(modelo);
            }

            // Empresa suspendida (por ejemplo, contrato vencido): ninguna de sus cuentas ingresa
            if (usuario.EmpresaId.HasValue &&
                !await _contexto.Empresas.AnyAsync(e => e.Id == usuario.EmpresaId && e.Activa))
            {
                await RegistrarAccesoAsync("Ingreso rechazado", "Empresa suspendida", usuario);
                ModelState.AddModelError(string.Empty, "El servicio de su empresa se encuentra suspendido. Contacte al administrador.");
                return View(modelo);
            }

            await _signInManager.SignInAsync(usuario, isPersistent: modelo.Recordarme);
            await RegistrarAccesoAsync("Ingreso", null, usuario);

            // Solo se redirige a rutas locales (evita redirecciones a sitios externos)
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CerrarSesion()
        {
            // Se registra antes de cerrar: la sesión todavía identifica a la cuenta
            await _eventos.RegistrarAsync(AmbitoAuditoria.Acceso, "Cierre de sesión", null, "Cuenta");
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        [Authorize]
        [HttpGet]
        public IActionResult CambiarPassword()
        {
            ViewData["EsObligatorio"] = User.HasClaim(NombresClaim.DebeCambiarPassword, "true");
            return View(new CambiarPasswordViewModel());
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarPassword(CambiarPasswordViewModel modelo)
        {
            ViewData["EsObligatorio"] = User.HasClaim(NombresClaim.DebeCambiarPassword, "true");
            if (!ModelState.IsValid) return View(modelo);

            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return RedirectToAction(nameof(Login));

            if (modelo.PasswordActual == modelo.PasswordNueva)
            {
                ModelState.AddModelError(nameof(modelo.PasswordNueva),
                    "La nueva contraseña debe ser distinta de la actual.");
                return View(modelo);
            }

            var resultado = await _userManager.ChangePasswordAsync(
                usuario, modelo.PasswordActual, modelo.PasswordNueva);

            if (!resultado.Succeeded)
            {
                foreach (var error in resultado.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(modelo);
            }

            // La contraseña temporal deja de ser obligatoria cambiarla
            usuario.DebeCambiarPassword = false;
            await _userManager.UpdateAsync(usuario);

            // Regenera la cookie para que el claim de cambio obligatorio desaparezca
            await _signInManager.RefreshSignInAsync(usuario);
            await _eventos.RegistrarAsync(AmbitoAuditoria.Acceso, "Cambio de contraseña", null, "Cuenta");

            TempData["Exito"] = "La contraseña se actualizó correctamente.";
            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        public IActionResult AccesoDenegado() => View();
    }
}