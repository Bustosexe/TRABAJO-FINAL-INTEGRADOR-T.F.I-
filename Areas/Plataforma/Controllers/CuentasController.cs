using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StockSuper.Data;
using StockSuper.Models;
using StockSuper.Models.Enums;
using StockSuper.Models.ViewModels;
using StockSuper.Services;

namespace StockSuper.Areas.Plataforma.Controllers
{
    // Cuentas de acceso de las empresas clientes. No existe registro público: el
    // administrador de la plataforma crea cada cuenta (Dirección o sucursal), el sistema
    // genera una contraseña temporal y la cuenta debe cambiarla en su primer ingreso.
    // Las cuentas no se eliminan: se desactivan y se reactivan.
    [Area("Plataforma")]
    [Authorize(Roles = Roles.AdministradorPlataforma)]
    public class CuentasController : Controller
    {
        // Claves de TempData para mostrar una única vez la contraseña temporal generada
        private const string ClaveId = "CredencialId";
        private const string ClavePassword = "CredencialPassword";
        private const string ClaveReset = "CredencialReset";

        private readonly ApplicationDbContext _contexto;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ServicioCorreo _correo;
        private readonly ILogger<CuentasController> _logger;
        private readonly ServicioEventosAuditoria _eventos;

        public CuentasController(
            ApplicationDbContext contexto, UserManager<ApplicationUser> userManager,
            ServicioCorreo correo, ILogger<CuentasController> logger,
            ServicioEventosAuditoria eventos)
        {
            _contexto = contexto;
            _userManager = userManager;
            _correo = correo;
            _logger = logger;
            _eventos = eventos;
        }

        // Evento sobre una cuenta de cliente, realizado por el administrador de la plataforma
        private Task RegistrarCuentaAsync(string accion, ApplicationUser cuenta, string? detalle = null) =>
            _eventos.RegistrarAsync(AmbitoAuditoria.Plataforma, accion,
                detalle ?? $"{cuenta.Email} - {cuenta.NombreCuenta}", "Cuenta", cuenta.Id,
                cuenta.EmpresaId, cuenta.SucursalId);

        // Listado de cuentas de clientes. Las cuentas de administración de la plataforma
        // (sin empresa) no se gestionan desde esta pantalla.
        public async Task<IActionResult> Index(int? empresaId, string? buscar, string? estado)
        {
            // Al volver al listado se descartan las credenciales temporales pendientes de entrega
            TempData.Remove(ClaveId);
            TempData.Remove(ClavePassword);
            TempData.Remove(ClaveReset);

            // IgnoreQueryFilters: el administrador no pertenece a ninguna empresa y los filtros
            // globales le ocultarían la sucursal de cada cuenta. No se lee ningún dato de stock.
            var consulta = _contexto.Users.IgnoreQueryFilters().AsNoTracking()
                .Where(u => u.EmpresaId != null);

            if (empresaId.HasValue) consulta = consulta.Where(u => u.EmpresaId == empresaId);

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var texto = buscar.Trim();
                consulta = consulta.Where(u => u.NombreCuenta.Contains(texto)
                    || (u.Email != null && u.Email.Contains(texto)));
            }

            consulta = estado switch
            {
                "activas" => consulta.Where(u => u.Activo),
                "desactivadas" => consulta.Where(u => !u.Activo),
                "pendientes" => consulta.Where(u => u.Activo && u.DebeCambiarPassword),
                _ => consulta
            };

            var lista = await consulta
                .OrderBy(u => u.Empresa!.RazonSocial).ThenBy(u => u.SucursalId != null).ThenBy(u => u.NombreCuenta)
                .Select(u => new CuentaListadoViewModel
                {
                    Id = u.Id,
                    NombreCuenta = u.NombreCuenta,
                    Email = u.Email ?? string.Empty,
                    Empresa = u.Empresa!.RazonSocial,
                    EmpresaActiva = u.Empresa.Activa,
                    Sucursal = u.Sucursal != null ? u.Sucursal.Nombre : null,
                    SucursalCerrada = u.Sucursal != null && u.Sucursal.Estado == EstadoSucursal.Cerrada,
                    Activo = u.Activo,
                    DebeCambiarPassword = u.DebeCambiarPassword
                }).ToListAsync();

            ViewData["Empresas"] = new SelectList(
                await _contexto.Empresas.AsNoTracking().OrderBy(e => e.RazonSocial).ToListAsync(),
                "Id", "RazonSocial", empresaId);
            ViewData["Buscar"] = buscar;
            ViewData["Estado"] = estado;
            ViewData["EmpresaId"] = empresaId;
            return View(lista);
        }

        [HttpGet]
        public async Task<IActionResult> Crear(int? empresaId)
        {
            var modelo = new CuentaCrearViewModel { EmpresaId = empresaId };
            await CargarListasAsync(modelo);
            return View(modelo);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(CuentaCrearViewModel modelo)
        {
            modelo.Email = modelo.Email?.Trim().ToLowerInvariant() ?? string.Empty;
            modelo.NombreCuenta = string.IsNullOrWhiteSpace(modelo.NombreCuenta) ? null : modelo.NombreCuenta.Trim();

            Empresa? empresa = null;
            if (modelo.EmpresaId.HasValue)
            {
                empresa = await _contexto.Empresas.AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == modelo.EmpresaId && e.Activa);
                if (empresa == null)
                    ModelState.AddModelError(nameof(modelo.EmpresaId), "La empresa seleccionada no está disponible.");
            }

            var esSucursal = modelo.Tipo == TiposCuenta.Sucursal;
            if (!esSucursal && modelo.Tipo != TiposCuenta.Direccion)
                ModelState.AddModelError(nameof(modelo.Tipo), "El tipo de cuenta no es válido.");

            // La sucursal debe pertenecer a la empresa elegida y estar activa
            Sucursal? sucursal = null;
            if (esSucursal)
            {
                if (!modelo.SucursalId.HasValue)
                {
                    ModelState.AddModelError(nameof(modelo.SucursalId), "Seleccione la sucursal de la cuenta.");
                }
                else if (empresa != null)
                {
                    sucursal = await _contexto.Sucursales.IgnoreQueryFilters().AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Id == modelo.SucursalId
                            && s.EmpresaId == empresa.Id && s.Estado == EstadoSucursal.Activa);
                    if (sucursal == null)
                        ModelState.AddModelError(nameof(modelo.SucursalId), "La sucursal no pertenece a la empresa elegida o está cerrada.");
                }
            }

            if (!string.IsNullOrEmpty(modelo.Email) && await _userManager.FindByEmailAsync(modelo.Email) != null)
                ModelState.AddModelError(nameof(modelo.Email), "Ya existe una cuenta con ese correo electrónico.");

            if (!ModelState.IsValid || empresa == null)
            {
                await CargarListasAsync(modelo);
                return View(modelo);
            }

            var nombre = modelo.NombreCuenta
                ?? $"{empresa.RazonSocial} - {(sucursal != null ? sucursal.Nombre : "Dirección")}";
            if (nombre.Length > 150) nombre = nombre[..150];

            var usuario = new ApplicationUser
            {
                UserName = modelo.Email,
                Email = modelo.Email,
                EmailConfirmed = true,
                NombreCuenta = nombre,
                // Empresa y sucursal quedan fijas: viajan como claims y no se pueden cambiar
                EmpresaId = empresa.Id,
                SucursalId = sucursal?.Id,
                DebeCambiarPassword = true,
                Activo = true
            };

            var password = GenerarPasswordTemporal();
            var resultado = await _userManager.CreateAsync(usuario, password);

            if (resultado.Succeeded)
            {
                var rol = sucursal != null ? Roles.UsuarioSucursal : Roles.DirectorEmpresa;
                resultado = await _userManager.AddToRoleAsync(usuario, rol);

                // Una cuenta sin rol no sirve: si falla la asignación, se deshace el alta
                if (!resultado.Succeeded) await _userManager.DeleteAsync(usuario);
            }

            if (!resultado.Succeeded)
            {
                foreach (var error in resultado.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                await CargarListasAsync(modelo);
                return View(modelo);
            }

            _logger.LogInformation("Cuenta {Email} creada por {Administrador}", usuario.Email, User.Identity?.Name);
            await RegistrarCuentaAsync("Cuenta creada", usuario);

            GuardarCredenciales(usuario.Id, password, esRestablecimiento: false);
            return RedirectToAction(nameof(Credenciales));
        }

        // Muestra la contraseña temporal recién generada, con las opciones para entregarla.
        // Deja de estar disponible cuando el administrador vuelve al listado.
        [HttpGet]
        public async Task<IActionResult> Credenciales()
        {
            // Peek lee sin consumir: la pantalla tolera recargas (F5) y cargas repetidas del
            // navegador. Las credenciales se descartan al volver al listado de cuentas.
            // ToString y no "as string": al releer la cookie, TempData convierte en Guid los textos
            // con formato de GUID (como el Id de usuario de Identity) y el "as string" daba nulo.
            var id = TempData.Peek(ClaveId)?.ToString();
            var password = TempData.Peek(ClavePassword)?.ToString();
            var esReset = TempData.Peek(ClaveReset)?.ToString() == "1";

            var usuario = id == null ? null : await BuscarCuentaAsync(id);
            if (usuario == null || string.IsNullOrEmpty(password))
            {
                TempData["Error"] = "Las credenciales ya no están disponibles. Restablezca la contraseña de la cuenta para generar una nueva.";
                return RedirectToAction(nameof(Index));
            }

            var mensaje = ArmarMensaje(usuario, password);
            return View(new CredencialesViewModel
            {
                UsuarioId = usuario.Id,
                NombreCuenta = usuario.NombreCuenta,
                Email = usuario.Email ?? string.Empty,
                PasswordTemporal = password,
                UrlIngreso = UrlIngreso(),
                Mensaje = mensaje,
                // Enlace wa.me con el texto precargado: no requiere la API de WhatsApp
                EnlaceWhatsApp = "https://wa.me/?text=" + Uri.EscapeDataString(mensaje),
                EsRestablecimiento = esReset,
                CorreoConfigurado = _correo.Configurado
            });
        }

        // Envía las credenciales por correo. Solo funciona con la contraseña temporal vigente
        // de la cuenta: así esta acción no puede usarse para enviar otro contenido.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarCredenciales(string id, string? password, string? destino)
        {
            var usuario = await BuscarCuentaAsync(id);
            if (usuario == null) return NotFound();

            if (string.IsNullOrEmpty(password) || !usuario.DebeCambiarPassword
                || !await _userManager.CheckPasswordAsync(usuario, password))
            {
                TempData["Error"] = "Las credenciales ya no están vigentes. Restablezca la contraseña de la cuenta.";
                return RedirectToAction(nameof(Index));
            }

            // Las credenciales se vuelven a guardar para seguir mostrándolas tras el envío
            GuardarCredenciales(usuario.Id, password, esRestablecimiento: false);
            destino = destino?.Trim();

            if (!_correo.Configurado)
            {
                TempData["Error"] = "El envío de correos todavía no está configurado en el servidor.";
            }
            else if (string.IsNullOrWhiteSpace(destino) || destino.Length > 100
                || !new EmailAddressAttribute().IsValid(destino))
            {
                TempData["Error"] = "Ingrese un correo electrónico de destino válido.";
            }
            else
            {
                try
                {
                    await _correo.EnviarAsync(destino, "Acceso a StockSuper", ArmarMensaje(usuario, password));
                    _logger.LogInformation("Credenciales de {Email} enviadas a {Destino}", usuario.Email, destino);
                    TempData["Exito"] = $"Las credenciales se enviaron a {destino}.";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "No se pudieron enviar las credenciales a {Destino}", destino);
                    TempData["Error"] = "No se pudo enviar el correo. Verifique la dirección e intente nuevamente.";
                }
            }

            return RedirectToAction(nameof(Credenciales));
        }

        [HttpGet]
        public async Task<IActionResult> Editar(string id)
        {
            var usuario = await BuscarCuentaAsync(id);
            if (usuario == null) return NotFound();

            var modelo = new CuentaEditarViewModel
            {
                Id = usuario.Id,
                Email = usuario.Email ?? string.Empty,
                NombreCuenta = usuario.NombreCuenta
            };
            await CompletarEdicionAsync(modelo, usuario);
            return View(modelo);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(string id, CuentaEditarViewModel modelo)
        {
            var usuario = await BuscarCuentaAsync(id);
            if (usuario == null) return NotFound();

            modelo.Id = id;
            modelo.Email = modelo.Email?.Trim().ToLowerInvariant() ?? string.Empty;
            modelo.NombreCuenta = modelo.NombreCuenta?.Trim() ?? string.Empty;

            if (!string.IsNullOrEmpty(modelo.Email))
            {
                var existente = await _userManager.FindByEmailAsync(modelo.Email);
                if (existente != null && existente.Id != usuario.Id)
                    ModelState.AddModelError(nameof(modelo.Email), "Ya existe otra cuenta con ese correo electrónico.");
            }

            if (ModelState.IsValid)
            {
                var huboCambios = usuario.Email != modelo.Email || usuario.NombreCuenta != modelo.NombreCuenta;

                // El correo es también el usuario de ingreso; UpdateAsync recalcula los valores normalizados
                usuario.Email = modelo.Email;
                usuario.UserName = modelo.Email;
                usuario.NombreCuenta = modelo.NombreCuenta;

                var resultado = await _userManager.UpdateAsync(usuario);
                if (resultado.Succeeded)
                {
                    // El nombre viaja en la sesión: se renueva el sello para que tome el dato nuevo
                    if (huboCambios) await _userManager.UpdateSecurityStampAsync(usuario);

                    if (huboCambios) await RegistrarCuentaAsync("Cuenta modificada", usuario);

                    TempData["Exito"] = $"La cuenta '{usuario.NombreCuenta}' se actualizó correctamente.";
                    return RedirectToAction(nameof(Index), new { empresaId = usuario.EmpresaId });
                }

                foreach (var error in resultado.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
            }

            await CompletarEdicionAsync(modelo, usuario);
            return View(modelo);
        }

        // Genera una nueva contraseña temporal: cierra las sesiones abiertas, levanta el
        // bloqueo por intentos fallidos y obliga a cambiarla en el próximo ingreso.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Restablecer(string id)
        {
            var usuario = await BuscarCuentaAsync(id);
            if (usuario == null) return NotFound();

            var password = GenerarPasswordTemporal();
            var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);
            var resultado = await _userManager.ResetPasswordAsync(usuario, token, password);

            if (!resultado.Succeeded)
            {
                TempData["Error"] = "No se pudo restablecer la contraseña. Intente nuevamente.";
                return RedirectToAction(nameof(Index), new { empresaId = usuario.EmpresaId });
            }

            usuario.DebeCambiarPassword = true;
            await _userManager.UpdateAsync(usuario);
            await _userManager.SetLockoutEndDateAsync(usuario, null);
            await _userManager.ResetAccessFailedCountAsync(usuario);

            _logger.LogInformation("Contraseña de {Email} restablecida por {Administrador}", usuario.Email, User.Identity?.Name);
            await RegistrarCuentaAsync("Contraseña restablecida", usuario);

            GuardarCredenciales(usuario.Id, password, esRestablecimiento: true);
            return RedirectToAction(nameof(Credenciales));
        }

        // Desactivar o reactivar. Una cuenta de una sucursal cerrada no se puede reactivar.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(string id)
        {
            var usuario = await BuscarCuentaAsync(id);
            if (usuario == null) return NotFound();

            var reactivando = !usuario.Activo;

            if (reactivando && usuario.SucursalId.HasValue
                && await _contexto.Sucursales.IgnoreQueryFilters()
                    .AnyAsync(s => s.Id == usuario.SucursalId && s.Estado == EstadoSucursal.Cerrada))
            {
                TempData["Error"] = "La sucursal de esta cuenta está cerrada. Reabra la sucursal antes de reactivar la cuenta.";
                return RedirectToAction(nameof(Index), new { empresaId = usuario.EmpresaId });
            }

            usuario.Activo = reactivando;
            await _userManager.UpdateAsync(usuario);

            // Al desactivar se invalida la sesión abierta (se cierra en la próxima revalidación)
            if (!reactivando) await _userManager.UpdateSecurityStampAsync(usuario);

            _logger.LogInformation("Cuenta {Email} {Accion} por {Administrador}",
                usuario.Email, reactivando ? "reactivada" : "desactivada", User.Identity?.Name);
            await RegistrarCuentaAsync(reactivando ? "Cuenta reactivada" : "Cuenta desactivada", usuario);

            TempData["Exito"] = reactivando
                ? $"La cuenta '{usuario.NombreCuenta}' fue reactivada y puede volver a ingresar."
                : $"La cuenta '{usuario.NombreCuenta}' fue desactivada y ya no puede ingresar.";
            return RedirectToAction(nameof(Index), new { empresaId = usuario.EmpresaId });
        }

        // Solo se gestionan cuentas de clientes; las de administración de la plataforma quedan fuera
        private async Task<ApplicationUser?> BuscarCuentaAsync(string? id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var usuario = await _userManager.FindByIdAsync(id);
            return usuario?.EmpresaId == null ? null : usuario;
        }

        private void GuardarCredenciales(string usuarioId, string password, bool esRestablecimiento)
        {
            TempData[ClaveId] = usuarioId;
            TempData[ClavePassword] = password;
            TempData[ClaveReset] = esRestablecimiento ? "1" : "0";
        }

        private string UrlIngreso() =>
            Url.Action("Login", "Cuenta", new { area = "" }, Request.Scheme) ?? string.Empty;

        // Texto que se entrega al cliente por correo o WhatsApp
        private string ArmarMensaje(ApplicationUser usuario, string password) =>
            "Acceso a StockSuper\n" +
            $"Cuenta: {usuario.NombreCuenta}\n" +
            $"Ingreso: {UrlIngreso()}\n" +
            $"Usuario: {usuario.Email}\n" +
            $"Contraseña temporal: {password}\n\n" +
            "En el primer ingreso el sistema le pedirá que cambie la contraseña.";

        // Contraseña temporal de 12 caracteres con generador criptográfico. Garantiza
        // mayúscula, minúscula y número (política de Identity) y evita caracteres que
        // se confunden al dictarlos o copiarlos (0/O, 1/l/I).
        private static string GenerarPasswordTemporal()
        {
            const string mayusculas = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string minusculas = "abcdefghijkmnpqrstuvwxyz";
            const string digitos = "23456789";
            const string todos = mayusculas + minusculas + digitos;

            var caracteres = new char[12];
            caracteres[0] = mayusculas[RandomNumberGenerator.GetInt32(mayusculas.Length)];
            caracteres[1] = minusculas[RandomNumberGenerator.GetInt32(minusculas.Length)];
            caracteres[2] = digitos[RandomNumberGenerator.GetInt32(digitos.Length)];
            for (var i = 3; i < caracteres.Length; i++)
                caracteres[i] = todos[RandomNumberGenerator.GetInt32(todos.Length)];

            // Mezcla para que los tres primeros no queden siempre en el mismo orden
            for (var i = caracteres.Length - 1; i > 0; i--)
            {
                var j = RandomNumberGenerator.GetInt32(i + 1);
                (caracteres[i], caracteres[j]) = (caracteres[j], caracteres[i]);
            }

            return new string(caracteres);
        }

        private async Task CargarListasAsync(CuentaCrearViewModel modelo)
        {
            modelo.Empresas = await _contexto.Empresas.AsNoTracking().Where(e => e.Activa)
                .OrderBy(e => e.RazonSocial)
                .Select(e => new SelectListItem(e.RazonSocial, e.Id.ToString())).ToListAsync();

            // Todas las sucursales activas; el formulario las filtra según la empresa elegida
            modelo.Sucursales = await _contexto.Sucursales.IgnoreQueryFilters().AsNoTracking()
                .Where(s => s.Estado == EstadoSucursal.Activa && s.Empresa!.Activa)
                .OrderByDescending(s => s.EsCasaCentral).ThenBy(s => s.Codigo)
                .Select(s => new SucursalOpcion
                {
                    Id = s.Id,
                    EmpresaId = s.EmpresaId,
                    Texto = s.Codigo + " - " + s.Nombre
                }).ToListAsync();
        }

        private async Task CompletarEdicionAsync(CuentaEditarViewModel modelo, ApplicationUser usuario)
        {
            modelo.EmpresaNombre = await _contexto.Empresas.AsNoTracking()
                .Where(e => e.Id == usuario.EmpresaId).Select(e => e.RazonSocial)
                .FirstOrDefaultAsync() ?? string.Empty;

            modelo.Alcance = "Dirección de empresa (todas las sucursales)";
            if (usuario.SucursalId.HasValue)
            {
                var sucursal = await _contexto.Sucursales.IgnoreQueryFilters().AsNoTracking()
                    .Where(s => s.Id == usuario.SucursalId).Select(s => s.Codigo + " - " + s.Nombre)
                    .FirstOrDefaultAsync();
                modelo.Alcance = "Sucursal " + (sucursal ?? string.Empty);
            }
        }
    }
}
