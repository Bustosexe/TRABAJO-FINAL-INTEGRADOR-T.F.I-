namespace StockSuper.Services
{
    // Manejo de adjuntos del sistema (imágenes de productos). Valida tamaño,
    // extensión y firma binaria del archivo, y lo guarda con un nombre generado
    // (GUID) para que nadie pueda pisar archivos ni subir rutas manipuladas.
    public class ServicioArchivos
    {
        private const long TamanoMaximoBytes = 2 * 1024 * 1024;
        private const string RutaPublicaProductos = "/uploads/productos/";
        private static readonly string[] ExtensionesPermitidas = { ".jpg", ".jpeg", ".png", ".webp" };

        private readonly IWebHostEnvironment _entorno;

        public ServicioArchivos(IWebHostEnvironment entorno) => _entorno = entorno;

        // Devuelve la ruta pública guardada, o un mensaje de error si el archivo no es válido
        public async Task<(string? Ruta, string? Error)> GuardarImagenProductoAsync(IFormFile archivo)
        {
            if (archivo.Length == 0)
                return (null, "El archivo seleccionado está vacío.");

            if (archivo.Length > TamanoMaximoBytes)
                return (null, "La imagen no puede superar los 2 MB.");

            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (!ExtensionesPermitidas.Contains(extension))
                return (null, "Formato no permitido. Utilice una imagen JPG, PNG o WEBP.");

            // Firma binaria: el contenido debe corresponder a la extensión declarada
            var cabecera = new byte[12];
            await using (var lectura = archivo.OpenReadStream())
            {
                var leidos = await lectura.ReadAtLeastAsync(cabecera, cabecera.Length, throwOnEndOfStream: false);
                if (leidos < cabecera.Length || !FirmaValida(cabecera, extension))
                    return (null, "El contenido del archivo no corresponde a una imagen válida.");
            }

            var carpeta = Path.Combine(_entorno.WebRootPath, "uploads", "productos");
            Directory.CreateDirectory(carpeta);

            var nombreArchivo = $"{Guid.NewGuid():N}{extension}";
            await using (var destino = new FileStream(Path.Combine(carpeta, nombreArchivo), FileMode.Create))
            {
                await archivo.CopyToAsync(destino);
            }

            return (RutaPublicaProductos + nombreArchivo, null);
        }

        // Elimina el archivo físico. Solo actúa sobre la carpeta de imágenes de productos.
        public void EliminarImagen(string? rutaPublica)
        {
            if (string.IsNullOrEmpty(rutaPublica) || !rutaPublica.StartsWith(RutaPublicaProductos))
                return;

            // GetFileName descarta cualquier segmento de ruta (evita salir de la carpeta)
            var fisica = Path.Combine(_entorno.WebRootPath, "uploads", "productos", Path.GetFileName(rutaPublica));
            if (File.Exists(fisica)) File.Delete(fisica);
        }

        // ---- Remitos en PDF ----
        // Se guardan fuera de wwwroot (App_Data/remitos/{empresa}/{guid}.pdf): ninguna dirección
        // pública los sirve. Para verlos hay que pasar por PalletsController.Remito.
        private const long TamanoMaximoRemitoBytes = 5 * 1024 * 1024;
        private static readonly System.Text.RegularExpressions.Regex RutaRemitoValida =
            new(@"^\d{1,10}/[0-9a-f]{32}\.pdf$");

        private string CarpetaRemitos => Path.Combine(_entorno.ContentRootPath, "App_Data", "remitos");

        // Devuelve la ruta relativa guardada, o un mensaje de error si el archivo no es válido
        public async Task<(string? Ruta, string? Error)> GuardarRemitoAsync(IFormFile archivo, int empresaId)
        {
            if (archivo.Length == 0)
                return (null, "El archivo seleccionado está vacío.");

            if (archivo.Length > TamanoMaximoRemitoBytes)
                return (null, "El remito no puede superar los 5 MB.");

            if (!string.Equals(Path.GetExtension(archivo.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
                return (null, "El remito debe ser un archivo PDF.");

            // Firma binaria: todo PDF comienza con "%PDF-"
            var cabecera = new byte[5];
            await using (var lectura = archivo.OpenReadStream())
            {
                var leidos = await lectura.ReadAtLeastAsync(cabecera, cabecera.Length, throwOnEndOfStream: false);
                if (leidos < cabecera.Length || System.Text.Encoding.ASCII.GetString(cabecera) != "%PDF-")
                    return (null, "El contenido del archivo no corresponde a un PDF válido.");
            }

            // Nombre generado por el servidor: el nombre original nunca se usa como ruta
            var nombre = $"{Guid.NewGuid():N}.pdf";
            var carpeta = Path.Combine(CarpetaRemitos, empresaId.ToString());
            Directory.CreateDirectory(carpeta);

            await using (var destino = new FileStream(Path.Combine(carpeta, nombre), FileMode.CreateNew))
            {
                await archivo.CopyToAsync(destino);
            }

            return ($"{empresaId}/{nombre}", null);
        }

        // Ruta física del remito, o nulo si la ruta guardada no es válida o el archivo no existe
        public string? RutaFisicaRemito(string? ruta)
        {
            if (string.IsNullOrEmpty(ruta) || !RutaRemitoValida.IsMatch(ruta)) return null;

            var baseRemitos = Path.GetFullPath(CarpetaRemitos);
            var fisica = Path.GetFullPath(Path.Combine(baseRemitos, ruta));

            // Nunca se sirve un archivo fuera de la carpeta de remitos
            if (!fisica.StartsWith(baseRemitos, StringComparison.OrdinalIgnoreCase)) return null;
            return File.Exists(fisica) ? fisica : null;
        }

        public void EliminarRemito(string? ruta)
        {
            var fisica = RutaFisicaRemito(ruta);
            if (fisica != null) File.Delete(fisica);
        }

        private static bool FirmaValida(byte[] c, string extension) => extension switch
        {
            ".jpg" or ".jpeg" => c[0] == 0xFF && c[1] == 0xD8 && c[2] == 0xFF,
            ".png" => c[0] == 0x89 && c[1] == 0x50 && c[2] == 0x4E && c[3] == 0x47,
            // WEBP: "RIFF" + 4 bytes de tamaño + "WEBP"
            ".webp" => c[0] == 'R' && c[1] == 'I' && c[2] == 'F' && c[3] == 'F'
                    && c[8] == 'W' && c[9] == 'E' && c[10] == 'B' && c[11] == 'P',
            _ => false
        };
    }
}