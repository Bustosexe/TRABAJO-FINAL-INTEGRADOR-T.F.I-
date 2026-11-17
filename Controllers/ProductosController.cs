using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StockSuper.Data;
using StockSuper.Models;
using StockSuper.Models.ViewModels;
using StockSuper.Services;

namespace StockSuper.Controllers
{
    // Catálogo de productos. La Dirección lo gestiona (alta, edición, baja lógica);
    // las sucursales solo lo consultan. Los productos no tienen ubicación ni
    // existencias propias: el stock vive en los pallets.
    [Authorize(Roles = Roles.DirectorEmpresa + "," + Roles.UsuarioSucursal)]
    public class ProductosController : Controller
    {
        private readonly ApplicationDbContext _contexto;
        private readonly ServicioAuditoria _auditoria;
        private readonly ServicioArchivos _archivos;

        public ProductosController(
            ApplicationDbContext contexto, ServicioAuditoria auditoria, ServicioArchivos archivos)
        {
            _contexto = contexto;
            _auditoria = auditoria;
            _archivos = archivos;
        }

        // Listado con búsqueda por SKU o nombre, filtro por categoría y por estado
        public async Task<IActionResult> Index(string? buscar, int? categoriaId, string? estado)
        {
            var consulta = _contexto.Productos.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var texto = buscar.Trim();
                consulta = consulta.Where(p => p.Nombre.Contains(texto) || p.CodigoSku.Contains(texto));
            }

            if (categoriaId.HasValue)
                consulta = consulta.Where(p => p.CategoriaId == categoriaId);

            // "bajominimo": productos críticos, con stock total por debajo del umbral de reposición
            consulta = estado switch
            {
                "activos" => consulta.Where(p => p.Activo),
                "inactivos" => consulta.Where(p => !p.Activo),
                "bajominimo" => consulta.Where(p => p.Activo && p.Pallets.Sum(x => x.CantidadActual) < p.StockMinimo),
                _ => consulta
            };

            var lista = await consulta.OrderBy(p => p.Nombre).Select(p => new ProductoListadoViewModel
            {
                Id = p.Id,
                CodigoSku = p.CodigoSku,
                Nombre = p.Nombre,
                Categoria = p.Categoria!.Nombre,
                Marca = p.Marca!.Nombre,
                Proveedor = p.Proveedor!.RazonSocial,
                PrecioVenta = p.PrecioVenta,
                StockMinimo = p.StockMinimo,
                StockActual = p.Pallets.Sum(x => x.CantidadActual),
                Activo = p.Activo,
                ImagenRuta = p.ImagenRuta,
                UsuarioCreacionId = p.UsuarioCreacionId,
                UsuarioModificacionId = p.UsuarioModificacionId,
                FechaCreacion = p.FechaCreacion,
                FechaModificacion = p.FechaModificacion
            }).ToListAsync();

            // Última acción de auditoría de cada fila (una sola consulta de nombres)
            var nombres = await _auditoria.NombresAsync(
                lista.SelectMany(i => new[] { i.UsuarioCreacionId, i.UsuarioModificacionId }));
            foreach (var item in lista)
            {
                var usuarioId = item.UsuarioModificacionId ?? item.UsuarioCreacionId;
                var fecha = (item.FechaModificacion ?? item.FechaCreacion).AHoraArgentina();
                var cuenta = usuarioId != null && nombres.TryGetValue(usuarioId, out var n) ? n : "Sistema";
                item.UltimaAccion = $"{cuenta} - {fecha:dd/MM/yyyy HH:mm}";
            }

            ViewData["Buscar"] = buscar;
            ViewData["Estado"] = estado;
            ViewData["CategoriaId"] = categoriaId;
            ViewData["Categorias"] = new SelectList(
                await _contexto.Categorias.AsNoTracking().OrderBy(c => c.Nombre).ToListAsync(),
                "Id", "Nombre", categoriaId);
            return View(lista);
        }

        // Ficha del producto con el stock desglosado por pallet y sucursal
        public async Task<IActionResult> Detalle(int id)
        {
            var producto = await _contexto.Productos.AsNoTracking()
                .Include(p => p.Categoria).Include(p => p.Marca).Include(p => p.Proveedor)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (producto == null) return NotFound();

            var pallets = await _contexto.Pallets.AsNoTracking()
                .Where(x => x.ProductoId == id && x.CantidadActual > 0)
                .OrderBy(x => x.FechaVencimiento == null).ThenBy(x => x.FechaVencimiento)
                .Select(x => new PalletDetalleItem
                {
                    Sucursal = x.Sucursal!.Nombre,
                    CodigoPallet = x.CodigoPallet,
                    Sector = x.SectorDeposito!.NombreSector,
                    Pasillo = x.SectorDeposito.Pasillo,
                    Estante = x.SectorDeposito.Estante,
                    CantidadActual = x.CantidadActual,
                    FechaVencimiento = x.FechaVencimiento
                }).ToListAsync();

            return View(new ProductoDetalleViewModel
            {
                Producto = producto,
                Pallets = pallets,
                StockTotal = pallets.Sum(x => x.CantidadActual),
                Auditoria = await _auditoria.ArmarPieAsync(producto)
            });
        }

        [Authorize(Roles = Roles.DirectorEmpresa)]
        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            var modelo = new ProductoFormViewModel();
            await CargarListasAsync(modelo);
            return View("Formulario", modelo);
        }

        [Authorize(Roles = Roles.DirectorEmpresa)]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(ProductoFormViewModel modelo)
        {
            Normalizar(modelo);
            await ValidarReglasAsync(modelo, proveedorActualId: null);

            // Primero se valida todo; el archivo se guarda recién cuando el resto es correcto
            string? rutaImagen = null;
            if (ModelState.IsValid && modelo.ImagenArchivo is { Length: > 0 })
            {
                var (ruta, error) = await _archivos.GuardarImagenProductoAsync(modelo.ImagenArchivo);
                if (error != null) ModelState.AddModelError(nameof(modelo.ImagenArchivo), error);
                rutaImagen = ruta;
            }

            if (!ModelState.IsValid)
            {
                await CargarListasAsync(modelo);
                return View("Formulario", modelo);
            }

            var producto = new Producto
            {
                CodigoSku = modelo.CodigoSku,
                Nombre = modelo.Nombre,
                Descripcion = modelo.Descripcion,
                PrecioVenta = modelo.PrecioVenta!.Value,
                StockMinimo = modelo.StockMinimo!.Value,
                CategoriaId = modelo.CategoriaId!.Value,
                MarcaId = modelo.MarcaId!.Value,
                ProveedorId = modelo.ProveedorId!.Value,
                ImagenRuta = rutaImagen,
                Activo = true
            };

            try
            {
                _contexto.Productos.Add(producto);
                await _contexto.SaveChangesAsync();
            }
            catch
            {
                // Si falla el guardado no deben quedar archivos huérfanos en el servidor
                _archivos.EliminarImagen(rutaImagen);
                throw;
            }

            TempData["Exito"] = $"El producto '{producto.Nombre}' se registró correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = Roles.DirectorEmpresa)]
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var producto = await _contexto.Productos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (producto == null) return NotFound();

            var modelo = new ProductoFormViewModel
            {
                Id = producto.Id,
                CodigoSku = producto.CodigoSku,
                Nombre = producto.Nombre,
                Descripcion = producto.Descripcion,
                PrecioVenta = producto.PrecioVenta,
                StockMinimo = producto.StockMinimo,
                CategoriaId = producto.CategoriaId,
                MarcaId = producto.MarcaId,
                ProveedorId = producto.ProveedorId,
                ImagenRuta = producto.ImagenRuta
            };

            await CargarListasAsync(modelo);
            ViewData["Pie"] = await _auditoria.ArmarPieAsync(producto);
            return View("Formulario", modelo);
        }

        // Sobrecarga con ViewModel: no choca con el GET porque los parámetros son distintos
        [Authorize(Roles = Roles.DirectorEmpresa)]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, ProductoFormViewModel modelo)
        {
            var producto = await _contexto.Productos.FirstOrDefaultAsync(p => p.Id == id);
            if (producto == null) return NotFound();

            modelo.Id = id;
            Normalizar(modelo);
            await ValidarReglasAsync(modelo, proveedorActualId: producto.ProveedorId);

            string? rutaNueva = null;
            if (ModelState.IsValid && modelo.ImagenArchivo is { Length: > 0 })
            {
                var (ruta, error) = await _archivos.GuardarImagenProductoAsync(modelo.ImagenArchivo);
                if (error != null) ModelState.AddModelError(nameof(modelo.ImagenArchivo), error);
                rutaNueva = ruta;
            }

            if (!ModelState.IsValid)
            {
                modelo.ImagenRuta = producto.ImagenRuta;
                await CargarListasAsync(modelo);
                ViewData["Pie"] = await _auditoria.ArmarPieAsync(producto);
                return View("Formulario", modelo);
            }

            var rutaAnterior = producto.ImagenRuta;

            producto.CodigoSku = modelo.CodigoSku;
            producto.Nombre = modelo.Nombre;
            producto.Descripcion = modelo.Descripcion;
            producto.PrecioVenta = modelo.PrecioVenta!.Value;
            producto.StockMinimo = modelo.StockMinimo!.Value;
            producto.CategoriaId = modelo.CategoriaId!.Value;
            producto.MarcaId = modelo.MarcaId!.Value;
            producto.ProveedorId = modelo.ProveedorId!.Value;

            // Imagen: la nueva reemplaza a la anterior; o se quita la existente
            if (rutaNueva != null) producto.ImagenRuta = rutaNueva;
            else if (modelo.QuitarImagen) producto.ImagenRuta = null;

            try
            {
                await _contexto.SaveChangesAsync();
            }
            catch
            {
                _archivos.EliminarImagen(rutaNueva);
                throw;
            }

            // El archivo viejo se borra recién cuando el cambio quedó guardado
            if (producto.ImagenRuta != rutaAnterior) _archivos.EliminarImagen(rutaAnterior);

            TempData["Exito"] = $"El producto '{producto.Nombre}' se actualizó correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // Baja lógica: el producto inactivo conserva todo su historial de stock
        [Authorize(Roles = Roles.DirectorEmpresa)]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var producto = await _contexto.Productos.FirstOrDefaultAsync(p => p.Id == id);
            if (producto == null) return NotFound();

            producto.Activo = !producto.Activo;
            await _contexto.SaveChangesAsync();

            TempData["Exito"] = producto.Activo
                ? $"El producto '{producto.Nombre}' fue reactivado."
                : $"El producto '{producto.Nombre}' fue dado de baja.";
            return RedirectToAction(nameof(Index));
        }

        private static void Normalizar(ProductoFormViewModel modelo)
        {
            // El SKU se guarda en mayúsculas y sin espacios para evitar duplicados "ocultos"
            modelo.CodigoSku = modelo.CodigoSku?.Trim().ToUpperInvariant() ?? string.Empty;
            modelo.Nombre = modelo.Nombre?.Trim() ?? string.Empty;
            modelo.Descripcion = string.IsNullOrWhiteSpace(modelo.Descripcion) ? null : modelo.Descripcion.Trim();
        }

        // Reglas de negocio que no puede resolver una anotación. Los filtros globales
        // garantizan que las claves foráneas solo se validen contra datos de la propia empresa.
        private async Task ValidarReglasAsync(ProductoFormViewModel modelo, int? proveedorActualId)
        {
            if (!string.IsNullOrEmpty(modelo.CodigoSku) &&
                await _contexto.Productos.AnyAsync(p => p.CodigoSku == modelo.CodigoSku && p.Id != modelo.Id))
                ModelState.AddModelError(nameof(modelo.CodigoSku), "Ya existe un producto con ese código SKU.");

            if (modelo.CategoriaId.HasValue &&
                !await _contexto.Categorias.AnyAsync(c => c.Id == modelo.CategoriaId))
                ModelState.AddModelError(nameof(modelo.CategoriaId), "La categoría seleccionada no es válida.");

            if (modelo.MarcaId.HasValue &&
                !await _contexto.Marcas.AnyAsync(m => m.Id == modelo.MarcaId))
                ModelState.AddModelError(nameof(modelo.MarcaId), "La marca seleccionada no es válida.");

            // Solo proveedores activos; al editar se tolera el que el producto ya tenía
            if (modelo.ProveedorId.HasValue &&
                !await _contexto.Proveedores.AnyAsync(p =>
                    p.Id == modelo.ProveedorId && (p.Activo || p.Id == proveedorActualId)))
                ModelState.AddModelError(nameof(modelo.ProveedorId), "El proveedor seleccionado no está disponible.");
        }

        private async Task CargarListasAsync(ProductoFormViewModel modelo)
        {
            modelo.Categorias = await _contexto.Categorias.AsNoTracking().OrderBy(c => c.Nombre)
                .Select(c => new SelectListItem(c.Nombre, c.Id.ToString())).ToListAsync();

            modelo.Marcas = await _contexto.Marcas.AsNoTracking().OrderBy(m => m.Nombre)
                .Select(m => new SelectListItem(m.Nombre, m.Id.ToString())).ToListAsync();

            // Proveedores dados de baja no se ofrecen (salvo el que ya tiene el producto)
            modelo.Proveedores = await _contexto.Proveedores.AsNoTracking()
                .Where(p => p.Activo || p.Id == modelo.ProveedorId)
                .OrderBy(p => p.RazonSocial)
                .Select(p => new SelectListItem(p.RazonSocial, p.Id.ToString())).ToListAsync();
        }
    }
}