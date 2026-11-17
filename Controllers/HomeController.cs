using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockSuper.Data;
using StockSuper.Models;
using StockSuper.Models.Enums;
using StockSuper.Models.ViewModels;
using StockSuper.Services;

namespace StockSuper.Controllers
{
    // Panel de inicio. Los filtros globales del DbContext limitan cada consulta
    // a la empresa y sucursal del usuario, por eso las consultas de stock no
    // repiten condiciones de empresa o sucursal.
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _contexto;
        private readonly IUsuarioActual _usuarioActual;

        public HomeController(ApplicationDbContext contexto, IUsuarioActual usuarioActual)
        {
            _contexto = contexto;
            _usuarioActual = usuarioActual;
        }

        public async Task<IActionResult> Index()
        {
            var modelo = new DashboardViewModel();

            // Administrador de plataforma: solo datos de gestión de clientes.
            // Para él los filtros devuelven cero filas de stock por diseño.
            if (User.IsInRole(Roles.AdministradorPlataforma))
            {
                modelo.EsPlataforma = true;
                modelo.TotalEmpresas = await _contexto.Empresas.CountAsync();
                modelo.EmpresasActivas = await _contexto.Empresas.CountAsync(e => e.Activa);
                modelo.TotalSucursales = await _contexto.Sucursales.IgnoreQueryFilters().CountAsync();
                modelo.TotalCuentas = await _contexto.Users.CountAsync();
                return View(modelo);
            }

            var empresaId = _usuarioActual.EmpresaId;
            modelo.NombreEmpresa = await _contexto.Empresas
                .Where(e => e.Id == empresaId)
                .Select(e => e.RazonSocial)
                .FirstOrDefaultAsync() ?? string.Empty;

            // Con SucursalId asignado el filtro deja pasar solo esa sucursal
            if (_usuarioActual.SucursalId.HasValue)
            {
                modelo.NombreSucursal = await _contexto.Sucursales
                    .Select(s => s.Nombre).FirstOrDefaultAsync();
            }
            else
            {
                modelo.SucursalesActivas = await _contexto.Sucursales
                    .CountAsync(s => s.Estado == EstadoSucursal.Activa);
            }

            var hoy = FormatoFecha.HoyArgentina;
            var limiteAviso = hoy.AddDays(30);

            modelo.ProductosActivos = await _contexto.Productos.CountAsync(p => p.Activo);
            modelo.UnidadesEnStock = await _contexto.Pallets.SumAsync(p => p.CantidadActual);
            modelo.PalletsConStock = await _contexto.Pallets.CountAsync(p => p.CantidadActual > 0);

            // Insumo crítico: el stock total de sus pallets está bajo el mínimo definido
            modelo.ProductosBajoMinimo = await _contexto.Productos
                .Where(p => p.Activo)
                .CountAsync(p => p.Pallets.Sum(x => x.CantidadActual) < p.StockMinimo);

            modelo.PalletsPorVencer = await _contexto.Pallets.CountAsync(p =>
                p.CantidadActual > 0 && p.FechaVencimiento != null &&
                p.FechaVencimiento >= hoy && p.FechaVencimiento <= limiteAviso);

            modelo.PalletsVencidos = await _contexto.Pallets.CountAsync(p =>
                p.CantidadActual > 0 && p.FechaVencimiento != null &&
                p.FechaVencimiento < hoy);

            return View(modelo);
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}