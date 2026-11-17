using System.Text;
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
    // Panel de alertas: insumos críticos, mercadería vencida y mercadería por vencer.
    // Los filtros globales limitan cada lista a la empresa (o sucursal) del usuario.
    // Incluye el módulo de IA: informe ejecutivo de las alertas y redacción
    // del correo de pedido de reposición a un proveedor. La IA solo recibe los datos
    // que el usuario ya puede ver, por lo que respeta el mismo aislamiento por empresa.
    [Authorize(Roles = Roles.DirectorEmpresa + "," + Roles.UsuarioSucursal)]
    public class AlertasController : Controller
    {
        private const int MaximoFilas = 100;

        // Tope de renglones por lista que se envían a la IA (controla el tamaño del prompt)
        private const int MaximoRenglonesIa = 15;
        private static readonly int[] DiasPermitidos = { 7, 15, 30, 60 };

        private readonly ApplicationDbContext _contexto;
        private readonly IUsuarioActual _usuarioActual;
        private readonly ServicioIa _ia;
        private readonly ServicioCorreo _correo;
        private readonly ILogger<AlertasController> _logger;
        private readonly ServicioEventosAuditoria _eventos;

        public AlertasController(
            ApplicationDbContext contexto, IUsuarioActual usuarioActual,
            ServicioIa ia, ServicioCorreo correo, ILogger<AlertasController> logger,
            ServicioEventosAuditoria eventos)
        {
            _eventos = eventos;
            _contexto = contexto;
            _usuarioActual = usuarioActual;
            _ia = ia;
            _correo = correo;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int dias = 30) => View(await ConstruirAsync(dias));

        // IA: informe ejecutivo en lenguaje natural a partir de las alertas actuales.
        // Se invoca por AJAX y devuelve JSON para mostrarlo en un modal.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> InformeIa(int dias = 30, CancellationToken cancelacion = default)
        {
            var alertas = await ConstruirAsync(dias);
            var (empresa, alcance) = await ContextoAsync();

            const string instrucciones =
                "Sos un analista de inventario que asiste a una cadena de supermercados. " +
                "Redactá en español un informe ejecutivo breve (máximo 250 palabras) a partir de los datos de alertas de stock que recibís. " +
                "Estructura obligatoria, con estos tres títulos en mayúsculas: SITUACIÓN GENERAL, RIESGOS PRINCIPALES, ACCIONES RECOMENDADAS. " +
                "En ACCIONES RECOMENDADAS enumerá entre tres y cinco acciones concretas ordenadas por prioridad, mencionando productos y pallets por su nombre. " +
                "Usá únicamente los datos recibidos: no inventes cifras, productos ni proveedores. " +
                "Si no hay alertas, indicá que la situación es normal. " +
                "Tono profesional y directo. Texto plano: sin Markdown, sin asteriscos, sin emojis.";

            var datos = new StringBuilder();
            datos.AppendLine($"Empresa: {empresa}");
            datos.AppendLine($"Alcance: {alcance}");
            datos.AppendLine($"Fecha: {alertas.Hoy:dd/MM/yyyy}");
            datos.AppendLine();

            datos.AppendLine($"PRODUCTOS BAJO EL STOCK MÍNIMO: {alertas.ProductosCriticos.Count}");
            foreach (var p in alertas.ProductosCriticos.Take(MaximoRenglonesIa))
                datos.AppendLine($"- {p.Nombre} ({p.Categoria}), proveedor {p.Proveedor}: stock {p.StockActual}, mínimo {p.StockMinimo}, faltan {p.Faltante}");
            datos.AppendLine();

            datos.AppendLine($"PALLETS VENCIDOS CON EXISTENCIA: {alertas.PalletsVencidos.Count}");
            foreach (var p in alertas.PalletsVencidos.Take(MaximoRenglonesIa))
                datos.AppendLine($"- Pallet {p.CodigoPallet}, {p.Producto}, {p.Sucursal}, sector {p.Sector}: {p.CantidadActual} unidades, venció el {p.FechaVencimiento:dd/MM/yyyy}");
            datos.AppendLine();

            datos.AppendLine($"PALLETS QUE VENCEN EN LOS PRÓXIMOS {alertas.Dias} DÍAS: {alertas.PalletsPorVencer.Count}");
            foreach (var p in alertas.PalletsPorVencer.Take(MaximoRenglonesIa))
                datos.AppendLine($"- Pallet {p.CodigoPallet}, {p.Producto}, {p.Sucursal}, sector {p.Sector}: {p.CantidadActual} unidades, vence el {p.FechaVencimiento:dd/MM/yyyy}");

            var (texto, error) = await _ia.GenerarAsync(instrucciones, datos.ToString(), cancelacion);
            if (error != null) return Json(new { ok = false, error });

            _logger.LogInformation("Informe de alertas con IA generado por {Cuenta}", User.Identity?.Name);
            return Json(new { ok = true, texto });
        }

        // IA: redacta el correo de pedido de reposición para un proveedor, con sus productos
        // bajo el mínimo. Devuelve un borrador que la Dirección revisa y edita antes de enviar.
        [Authorize(Roles = Roles.DirectorEmpresa)]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> PedidoIa(int proveedorId, CancellationToken cancelacion = default)
        {
            // El filtro global garantiza que el proveedor sea de la empresa del usuario
            var proveedor = await _contexto.Proveedores.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == proveedorId && p.Activo, cancelacion);
            if (proveedor == null)
                return Json(new { ok = false, error = "El proveedor seleccionado no está disponible." });

            var productos = await _contexto.Productos.AsNoTracking()
                .Where(p => p.Activo && p.ProveedorId == proveedorId
                    && p.Pallets.Sum(x => x.CantidadActual) < p.StockMinimo)
                .OrderBy(p => p.Nombre)
                .Select(p => new
                {
                    p.CodigoSku,
                    p.Nombre,
                    p.StockMinimo,
                    StockActual = p.Pallets.Sum(x => x.CantidadActual)
                })
                .Take(50)
                .ToListAsync(cancelacion);

            if (productos.Count == 0)
                return Json(new { ok = false, error = "Este proveedor no tiene productos bajo el stock mínimo." });

            var (empresa, _) = await ContextoAsync();

            const string instrucciones =
                "Sos el asistente de compras de una cadena de supermercados. " +
                "Redactá en español un correo formal y breve dirigido a un proveedor para solicitar la reposición de los productos indicados. " +
                "Incluí la lista de productos con su código y la cantidad solicitada, pedí confirmación de disponibilidad, precio y fecha de entrega, y cerrá con un saludo en nombre de la empresa. " +
                "Usá únicamente los datos recibidos: no inventes precios, fechas, nombres de personas ni condiciones comerciales. " +
                "Formato obligatorio: la primera línea debe ser 'ASUNTO: ' seguida del asunto; luego una línea en blanco y el cuerpo del correo. " +
                "Texto plano: sin Markdown, sin asteriscos, sin emojis.";

            // A la IA no se le envían datos de contacto del proveedor (correo, CUIT, teléfono)
            var datos = new StringBuilder();
            datos.AppendLine($"Empresa que solicita: {empresa}");
            datos.AppendLine($"Proveedor: {proveedor.RazonSocial}");
            datos.AppendLine("Productos a reponer (cantidad solicitada = unidades necesarias para alcanzar el stock mínimo):");
            foreach (var p in productos)
                datos.AppendLine($"- {p.Nombre} (código {p.CodigoSku}): {Math.Max(1, p.StockMinimo - p.StockActual)} unidades");

            var (texto, error) = await _ia.GenerarAsync(instrucciones, datos.ToString(), cancelacion);
            if (error != null || texto == null) return Json(new { ok = false, error });

            // Se separa el asunto del cuerpo según el formato pedido en el System Prompt
            var asunto = $"Pedido de reposición - {empresa}";
            var cuerpo = texto;
            var finPrimeraLinea = texto.IndexOf('\n');
            if (texto.StartsWith("ASUNTO:", StringComparison.OrdinalIgnoreCase) && finPrimeraLinea > 0)
            {
                asunto = texto[7..finPrimeraLinea].Trim();
                cuerpo = texto[(finPrimeraLinea + 1)..].Trim();
            }

            return Json(new
            {
                ok = true,
                asunto,
                cuerpo,
                proveedor = proveedor.RazonSocial,
                destino = proveedor.Email,
                puedeEnviar = !string.IsNullOrWhiteSpace(proveedor.Email) && _correo.Configurado
            });
        }

        // Envía al proveedor el pedido ya revisado. El destino se toma siempre del correo
        // registrado del proveedor: el navegador no puede elegir a quién se le escribe.
        [Authorize(Roles = Roles.DirectorEmpresa)]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarPedido(int proveedorId, string? asunto, string? cuerpo)
        {
            asunto = asunto?.Trim();
            cuerpo = cuerpo?.Trim();

            if (string.IsNullOrEmpty(asunto) || asunto.Length > 150)
                return Json(new { ok = false, error = "El asunto es obligatorio y no puede superar los 150 caracteres." });
            if (string.IsNullOrEmpty(cuerpo) || cuerpo.Length > 5000)
                return Json(new { ok = false, error = "El mensaje es obligatorio y no puede superar los 5.000 caracteres." });

            var proveedor = await _contexto.Proveedores.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == proveedorId && p.Activo);
            if (proveedor == null)
                return Json(new { ok = false, error = "El proveedor seleccionado no está disponible." });
            if (string.IsNullOrWhiteSpace(proveedor.Email))
                return Json(new { ok = false, error = "El proveedor no tiene un correo electrónico registrado." });
            if (!_correo.Configurado)
                return Json(new { ok = false, error = "El envío de correos todavía no está configurado en el servidor." });

            try
            {
                await _correo.EnviarAsync(proveedor.Email, asunto, cuerpo);
                _logger.LogInformation("Pedido de reposición enviado por {Cuenta} al proveedor {Proveedor}",
                    User.Identity?.Name, proveedor.RazonSocial);
                await _eventos.RegistrarAsync(AmbitoAuditoria.Negocio, "Pedido enviado a proveedor",
                    $"{proveedor.RazonSocial}: {asunto}", "Proveedor", proveedor.Id.ToString());
                return Json(new { ok = true, mensaje = $"El pedido se envió a {proveedor.RazonSocial} ({proveedor.Email})." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo enviar el pedido al proveedor {Proveedor}", proveedor.RazonSocial);
                return Json(new { ok = false, error = "No se pudo enviar el correo. Intente nuevamente." });
            }
        }

        // Cálculo de alertas compartido por la pantalla y por el informe con IA
        private async Task<AlertasViewModel> ConstruirAsync(int dias)
        {
            if (!DiasPermitidos.Contains(dias)) dias = 30;

            var hoy = FormatoFecha.HoyArgentina;
            var limite = hoy.AddDays(dias);

            // Insumo crítico: stock total del producto por debajo de su mínimo
            var criticos = await _contexto.Productos.AsNoTracking()
                .Where(p => p.Activo && p.Pallets.Sum(x => x.CantidadActual) < p.StockMinimo)
                .Select(p => new AlertaProductoItem
                {
                    Id = p.Id,
                    Sku = p.CodigoSku,
                    Nombre = p.Nombre,
                    Categoria = p.Categoria!.Nombre,
                    ProveedorId = p.ProveedorId,
                    Proveedor = p.Proveedor!.RazonSocial,
                    StockMinimo = p.StockMinimo,
                    StockActual = p.Pallets.Sum(x => x.CantidadActual)
                })
                .OrderByDescending(p => p.StockMinimo - p.StockActual)
                .Take(MaximoFilas)
                .ToListAsync();

            var pallets = _contexto.Pallets.AsNoTracking()
                .Where(p => p.CantidadActual > 0 && p.FechaVencimiento != null);

            var vencidos = await pallets
                .Where(p => p.FechaVencimiento < hoy)
                .OrderBy(p => p.FechaVencimiento)
                .Take(MaximoFilas)
                .Select(p => new AlertaPalletItem
                {
                    Id = p.Id,
                    Sucursal = p.Sucursal!.Nombre,
                    CodigoPallet = p.CodigoPallet,
                    Producto = p.Producto!.Nombre,
                    Sku = p.Producto.CodigoSku,
                    Sector = p.SectorDeposito!.NombreSector,
                    CantidadActual = p.CantidadActual,
                    FechaVencimiento = p.FechaVencimiento!.Value
                })
                .ToListAsync();

            var porVencer = await pallets
                .Where(p => p.FechaVencimiento >= hoy && p.FechaVencimiento <= limite)
                .OrderBy(p => p.FechaVencimiento)
                .Take(MaximoFilas)
                .Select(p => new AlertaPalletItem
                {
                    Id = p.Id,
                    Sucursal = p.Sucursal!.Nombre,
                    CodigoPallet = p.CodigoPallet,
                    Producto = p.Producto!.Nombre,
                    Sku = p.Producto.CodigoSku,
                    Sector = p.SectorDeposito!.NombreSector,
                    CantidadActual = p.CantidadActual,
                    FechaVencimiento = p.FechaVencimiento!.Value
                })
                .ToListAsync();

            return new AlertasViewModel
            {
                Dias = dias,
                Hoy = hoy,
                ProductosCriticos = criticos,
                PalletsVencidos = vencidos,
                PalletsPorVencer = porVencer
            };
        }

        // Empresa y alcance (sucursal o todas) del usuario, para dar contexto a la IA
        private async Task<(string Empresa, string Alcance)> ContextoAsync()
        {
            var empresaId = _usuarioActual.EmpresaId;
            var empresa = await _contexto.Empresas.AsNoTracking()
                .Where(e => e.Id == empresaId).Select(e => e.RazonSocial)
                .FirstOrDefaultAsync() ?? "la empresa";

            var alcance = "todas las sucursales de la empresa";
            if (_usuarioActual.SucursalId.HasValue)
            {
                // Con SucursalId asignado, el filtro global deja pasar solo esa sucursal
                var sucursal = await _contexto.Sucursales.AsNoTracking()
                    .Select(s => s.Nombre).FirstOrDefaultAsync();
                if (sucursal != null) alcance = "sucursal " + sucursal;
            }

            return (empresa, alcance);
        }
    }
}
