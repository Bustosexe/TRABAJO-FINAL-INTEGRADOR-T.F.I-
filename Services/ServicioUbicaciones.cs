using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;

namespace StockSuper.Services
{
    // Resultado de validar una provincia y una localidad contra el listado oficial
    public record ResultadoUbicacion(string Provincia, string Localidad, string? ErrorProvincia, string? ErrorLocalidad)
    {
        // Agrega los mensajes de error (si los hay) al formulario
        public void AplicarErrores(ModelStateDictionary estado, string campoProvincia, string campoLocalidad)
        {
            if (ErrorProvincia != null) estado.AddModelError(campoProvincia, ErrorProvincia);
            if (ErrorLocalidad != null) estado.AddModelError(campoLocalidad, ErrorLocalidad);
        }
    }

    // Provincias y localidades de Argentina para los formularios de empresas y sucursales.
    // Las 24 jurisdicciones están fijas en el código. Las localidades se consultan al
    // servicio oficial Georef (datos.gob.ar, sin API Key) desde el servidor y se guardan
    // en memoria 7 días, de modo que cada provincia se pide una sola vez. Se validan en
    // el servidor: el navegador no puede enviar una localidad que no esté en el listado.
    public class ServicioUbicaciones
    {
        // Código oficial (INDEC) y nombre de cada jurisdicción, por orden alfabético
        private static readonly (string Id, string Nombre)[] ListaProvincias =
        {
            ("06", "Buenos Aires"), ("10", "Catamarca"), ("22", "Chaco"), ("26", "Chubut"),
            ("02", "Ciudad Autónoma de Buenos Aires"), ("14", "Córdoba"), ("18", "Corrientes"),
            ("30", "Entre Ríos"), ("34", "Formosa"), ("38", "Jujuy"), ("42", "La Pampa"),
            ("46", "La Rioja"), ("50", "Mendoza"), ("54", "Misiones"), ("58", "Neuquén"),
            ("62", "Río Negro"), ("66", "Salta"), ("70", "San Juan"), ("74", "San Luis"),
            ("78", "Santa Cruz"), ("82", "Santa Fe"), ("86", "Santiago del Estero"),
            ("94", "Tierra del Fuego"), ("90", "Tucumán")
        };

        // Opciones del desplegable de provincias (el valor guardado es el nombre)
        public static readonly IReadOnlyList<SelectListItem> Provincias =
            ListaProvincias.Select(p => new SelectListItem(p.Nombre, p.Nombre)).ToList();

        private static readonly StringComparer Orden = StringComparer.Create(new CultureInfo("es-AR"), true);

        private readonly HttpClient _http;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ServicioUbicaciones> _logger;

        public ServicioUbicaciones(HttpClient http, IMemoryCache cache, ILogger<ServicioUbicaciones> logger)
        {
            _http = http;
            _cache = cache;
            _logger = logger;
        }

        private static (string Id, string Nombre)? BuscarProvincia(string? nombre)
        {
            foreach (var provincia in ListaProvincias)
                if (string.Equals(provincia.Nombre, nombre?.Trim(), StringComparison.OrdinalIgnoreCase))
                    return provincia;
            return null;
        }

        // Localidades de la provincia, ordenadas. Nulo si la provincia no existe o si el
        // servicio oficial no responde: en ese caso el formulario permite escribirla.
        public async Task<IReadOnlyList<string>?> LocalidadesAsync(string? provincia, CancellationToken cancelacion = default)
        {
            var encontrada = BuscarProvincia(provincia);
            if (encontrada == null) return null;

            var clave = "localidades:" + encontrada.Value.Id;
            if (_cache.TryGetValue<IReadOnlyList<string>>(clave, out var guardadas) && guardadas != null)
                return guardadas;

            try
            {
                var url = "https://apis.datos.gob.ar/georef/api/localidades?provincia="
                    + encontrada.Value.Id + "&campos=nombre&max=5000";

                using var respuesta = await _http.GetAsync(url, cancelacion);
                if (!respuesta.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Georef respondió {Estado} al pedir las localidades de {Provincia}",
                        (int)respuesta.StatusCode, encontrada.Value.Nombre);
                    return null;
                }

                using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync(cancelacion));
                var nombres = new List<string>();

                if (documento.RootElement.TryGetProperty("localidades", out var arreglo)
                    && arreglo.ValueKind == JsonValueKind.Array)
                {
                    foreach (var elemento in arreglo.EnumerateArray())
                    {
                        if (elemento.TryGetProperty("nombre", out var n) && n.ValueKind == JsonValueKind.String
                            && !string.IsNullOrWhiteSpace(n.GetString()))
                            nombres.Add(n.GetString()!.Trim());
                    }
                }

                var lista = nombres.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, Orden).ToList();
                if (lista.Count == 0) return null;

                _cache.Set(clave, (IReadOnlyList<string>)lista, TimeSpan.FromDays(7));
                return lista;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                _logger.LogWarning(ex, "No se pudo consultar Georef para {Provincia}", encontrada.Value.Nombre);
                return null;
            }
        }

        // Valida la provincia y la localidad. Devuelve los valores con su escritura oficial.
        // Si el servicio oficial no responde, la localidad se acepta tal como se escribió
        // para no impedir el trabajo; el resto de las validaciones se mantiene.
        public async Task<ResultadoUbicacion> ValidarAsync(
            string? provincia, string? localidad, CancellationToken cancelacion = default)
        {
            var textoProvincia = provincia?.Trim() ?? string.Empty;
            var textoLocalidad = localidad?.Trim() ?? string.Empty;

            // Si está vacía, ya lo informa el atributo [Required] del formulario
            if (textoProvincia.Length == 0)
                return new ResultadoUbicacion(textoProvincia, textoLocalidad, null, null);

            var encontrada = BuscarProvincia(textoProvincia);
            if (encontrada == null)
                return new ResultadoUbicacion(textoProvincia, textoLocalidad,
                    "Seleccione una provincia de la lista.", null);

            var nombreProvincia = encontrada.Value.Nombre;
            if (textoLocalidad.Length == 0)
                return new ResultadoUbicacion(nombreProvincia, textoLocalidad, null, null);

            var localidades = await LocalidadesAsync(nombreProvincia, cancelacion);
            if (localidades == null)
                return new ResultadoUbicacion(nombreProvincia, textoLocalidad, null, null);

            var oficial = localidades.FirstOrDefault(x =>
                string.Equals(x, textoLocalidad, StringComparison.OrdinalIgnoreCase));

            return oficial != null
                ? new ResultadoUbicacion(nombreProvincia, oficial, null, null)
                : new ResultadoUbicacion(nombreProvincia, textoLocalidad, null,
                    $"La localidad no figura en el listado oficial de {nombreProvincia}. Elíjala del desplegable.");
        }
    }
}
