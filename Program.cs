using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StockSuper.Data;
using StockSuper.Models;
using StockSuper.Services;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'DefaultConnection'.");

// Identidad del usuario actual (empresa y sucursal) para filtros y auditoría
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioActual, UsuarioActual>();
builder.Services.AddScoped<ServicioAuditoria>();
builder.Services.AddScoped<ServicioEventosAuditoria>();
builder.Services.AddScoped<ServicioConsultaAuditoria>();
builder.Services.AddScoped<ServicioArchivos>();
builder.Services.AddScoped<ServicioCorreo>();
builder.Services.AddScoped<ServicioPdfReporte>();

// Módulo de IA: cliente HTTP con tiempo máximo de espera y resumen de stock para el chat
builder.Services.AddHttpClient<ServicioIa>(cliente => cliente.Timeout = TimeSpan.FromSeconds(40));
builder.Services.AddScoped<ServicioContextoStock>();

// Provincias y localidades oficiales (servicio Georef de datos.gob.ar), con caché en memoria
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<ServicioUbicaciones>(cliente => cliente.Timeout = TimeSpan.FromSeconds(15));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Identity sin interfaz por defecto: el ingreso y el cambio de contraseña los
// resuelve CuentaController, con vistas propias en español.
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.User.RequireUniqueEmail = true;

    // Política de contraseñas
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;

    // Bloqueo temporal tras 5 intentos fallidos
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>()
    .AddErrorDescriber<DescriptorErroresIdentity>();

// Sesión de larga duración: se renueva con el uso. Con "mantener la sesión
// iniciada" tildado, sobrevive al cierre del navegador.
builder.Services.ConfigureApplicationCookie(opciones =>
{
    opciones.ExpireTimeSpan = TimeSpan.FromDays(30);
    opciones.SlidingExpiration = true;
    opciones.LoginPath = "/Cuenta/Login";
    opciones.AccessDeniedPath = "/Cuenta/AccesoDenegado";
});
// Identity revalida la sesión contra la base de datos cada minuto (por defecto, cada 30).
// Así, al suspender una empresa o desactivar una cuenta, esa sesión se cierra enseguida.
builder.Services.Configure<SecurityStampValidatorOptions>(opciones =>
    opciones.ValidationInterval = TimeSpan.FromMinutes(1));


// Mensajes de error de conversión de datos en español (ej.: texto en un campo numérico)
builder.Services.AddControllersWithViews(opciones =>
{
    var mensajes = opciones.ModelBindingMessageProvider;
    mensajes.SetValueMustBeANumberAccessor(campo => $"El campo {campo} debe ser un número.");
    mensajes.SetAttemptedValueIsInvalidAccessor((valor, campo) => $"El valor '{valor}' no es válido para {campo}.");
    mensajes.SetUnknownValueIsInvalidAccessor(campo => $"El valor ingresado no es válido para {campo}.");
    mensajes.SetValueIsInvalidAccessor(valor => $"El valor '{valor}' no es válido.");
    mensajes.SetMissingBindRequiredValueAccessor(campo => $"Falta el valor del campo {campo}.");
});

// Licencia Community de QuestPDF (generación de PDF)
QuestPDF.Settings.License = LicenseType.Community;

var app = builder.Build();
// Carga inicial de roles, empresa de demostración y cuentas de prueba
using (var alcance = app.Services.CreateScope())
{
    await DbInicializador.InicializarAsync(alcance.ServiceProvider);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Cultura argentina: coma decimal, punto de miles y moneda "$" al leer y mostrar datos
var culturaArgentina = new CultureInfo("es-AR");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culturaArgentina),
    SupportedCultures = new[] { culturaArgentina },
    SupportedUICultures = new[] { culturaArgentina }
});

app.UseRouting();

app.UseAuthorization();

// Cambio obligatorio de contraseña: mientras la cuenta tenga contraseña temporal,
// solo puede acceder a la pantalla de cambio y al cierre de sesión.
app.Use(async (contexto, siguiente) =>
{
    var accion = contexto.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();

    if (accion != null
        && contexto.User.Identity?.IsAuthenticated == true
        && contexto.User.HasClaim(NombresClaim.DebeCambiarPassword, "true")
        && !(accion.ControllerName == "Cuenta"
             && (accion.ActionName == "CambiarPassword" || accion.ActionName == "CerrarSesion")))
    {
        contexto.Response.Redirect("/Cuenta/CambiarPassword");
        return;
    }

    await siguiente();
});

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();



app.Run();
