using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace StockSuper.Services
{
    // Mensaje de una conversación con la IA. Rol: "user" (persona) o "assistant" (IA).
    public record MensajeIa(string Rol, string Texto);

    // Integración con el proveedor de IA por HTTP directo (HttpClient), sin librerías de terceros.
    // Admite dos proveedores, elegidos por configuración (IA:Proveedor):
    //  - "Groq": API compatible con OpenAI (chat/completions). Es el valor por defecto.
    //  - "Gemini": API de Google (generateContent).
    // La API Key se lee de la configuración (User Secrets en desarrollo, configuración del
    // servidor en producción): nunca está en el código ni en el repositorio, viaja en un
    // encabezado y nunca llega al navegador.
    public class ServicioIa
    {
        // Máximo de modelos que se prueban en una misma consulta
        private const int MaximoIntentos = 4;

        // Último modelo que respondió bien: se reutiliza en las consultas siguientes
        private static string? _modeloEnUso;

        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly ILogger<ServicioIa> _logger;

        public ServicioIa(HttpClient http, IConfiguration config, ILogger<ServicioIa> logger)
        {
            _http = http;
            _config = config;
            _logger = logger;
        }

        private bool EsGemini =>
            string.Equals(_config["IA:Proveedor"], "Gemini", StringComparison.OrdinalIgnoreCase);

        private string? ApiKey
        {
            get
            {
                var clave = _config["IA:ApiKey"];
                // Compatibilidad con la clave cargada originalmente como Gemini:ApiKey
                if (string.IsNullOrWhiteSpace(clave) && EsGemini) clave = _config["Gemini:ApiKey"];
                return string.IsNullOrWhiteSpace(clave) ? null : clave;
            }
        }

        private string UrlBase
        {
            get
            {
                if (EsGemini) return "https://generativelanguage.googleapis.com/v1beta";
                var url = _config["IA:UrlBase"];
                return string.IsNullOrWhiteSpace(url) ? "https://api.groq.com/openai/v1" : url.TrimEnd('/');
            }
        }

        // Falso si todavía no se cargó la API Key: la pantalla avisa en lugar de fallar
        public bool Configurado => ApiKey != null;

        // Consulta de un solo mensaje: System Prompt (rol y reglas) más los datos del sistema
        public Task<(string? Texto, string? Error)> GenerarAsync(
            string instrucciones, string datos, CancellationToken cancelacion = default) =>
            ConversarAsync(instrucciones, new[] { new MensajeIa("user", datos) }, cancelacion);

        // Conversación de varios turnos (chat). Devuelve el texto generado o un mensaje de
        // error apto para mostrar al usuario.
        public async Task<(string? Texto, string? Error)> ConversarAsync(
            string instrucciones, IReadOnlyList<MensajeIa> mensajes, CancellationToken cancelacion = default)
        {
            if (!Configurado)
                return (null, "El asistente de IA todavía no está configurado en el servidor.");

            try
            {
                // Se prueba primero el modelo que ya funcionó o el configurado (IA:Modelo). Si no
                // está disponible, se consulta al proveedor qué modelos ofrece la clave: los
                // proveedores renuevan y retiran modelos con frecuencia.
                var candidatos = new List<string>();
                var preferido = _modeloEnUso ?? _config["IA:Modelo"];
                if (!string.IsNullOrWhiteSpace(preferido)) candidatos.Add(preferido);

                var yaListados = false;
                HttpStatusCode? ultimoEstado = null;

                for (var intento = 0; intento < MaximoIntentos; intento++)
                {
                    if (intento >= candidatos.Count)
                    {
                        if (yaListados) break;
                        yaListados = true;

                        foreach (var disponible in await ListarModelosAsync(cancelacion))
                            if (!candidatos.Contains(disponible)) candidatos.Add(disponible);

                        if (intento >= candidatos.Count) break;
                    }

                    var modelo = candidatos[intento];
                    var (estado, cuerpo) = await EnviarAsync(modelo, instrucciones, mensajes, cancelacion);
                    ultimoEstado = estado;

                    if (estado == HttpStatusCode.OK)
                    {
                        var texto = ExtraerTexto(cuerpo);
                        if (!string.IsNullOrWhiteSpace(texto))
                        {
                            if (_modeloEnUso != modelo)
                                _logger.LogInformation("Modelo de IA en uso: {Modelo}", modelo);
                            _modeloEnUso = modelo;
                            return (texto, null);
                        }

                        return (null, "El asistente de IA no devolvió contenido. Intente nuevamente.");
                    }

                    _logger.LogWarning("El proveedor de IA respondió {Estado} con el modelo {Modelo}: {Detalle}",
                        (int)estado, modelo, cuerpo.Length > 400 ? cuerpo[..400] : cuerpo);

                    // Clave rechazada: no tiene sentido probar otros modelos
                    if (estado == HttpStatusCode.Unauthorized) break;
                    if (_modeloEnUso == modelo) _modeloEnUso = null;
                }

                return (null, MensajeDeError(ultimoEstado));
            }
            catch (TaskCanceledException)
            {
                return (null, "El asistente de IA tardó demasiado en responder. Intente nuevamente.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "No se pudo conectar con el proveedor de IA");
                return (null, "No se pudo conectar con el asistente de IA. Verifique la conexión e intente nuevamente.");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Respuesta del proveedor de IA con formato inesperado");
                return (null, "El asistente de IA devolvió una respuesta inesperada. Intente nuevamente.");
            }
        }

        // Arma y envía la consulta en el formato de cada proveedor
        private async Task<(HttpStatusCode Estado, string Cuerpo)> EnviarAsync(
            string modelo, string instrucciones, IReadOnlyList<MensajeIa> mensajes, CancellationToken cancelacion)
        {
            HttpRequestMessage solicitud;

            if (EsGemini)
            {
                solicitud = new HttpRequestMessage(HttpMethod.Post,
                    $"{UrlBase}/models/{Uri.EscapeDataString(modelo)}:generateContent");
                solicitud.Headers.Add("x-goog-api-key", ApiKey);
                solicitud.Content = JsonContent.Create(new
                {
                    systemInstruction = new { parts = new[] { new { text = instrucciones } } },
                    // Gemini llama "model" al rol del asistente
                    contents = mensajes.Select(m => new
                    {
                        role = m.Rol == "assistant" ? "model" : "user",
                        parts = new[] { new { text = m.Texto } }
                    }),
                    generationConfig = new { temperature = 0.4 }
                });
            }
            else
            {
                var lista = new List<object> { new { role = "system", content = instrucciones } };
                lista.AddRange(mensajes.Select(m => (object)new
                {
                    role = m.Rol == "assistant" ? "assistant" : "user",
                    content = m.Texto
                }));

                solicitud = new HttpRequestMessage(HttpMethod.Post, $"{UrlBase}/chat/completions");
                solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
                // Temperatura baja: redacción sobria y apegada a los datos enviados
                solicitud.Content = JsonContent.Create(new { model = modelo, messages = lista, temperature = 0.4 });
            }

            using (solicitud)
            using (var respuesta = await _http.SendAsync(solicitud, cancelacion))
            {
                return (respuesta.StatusCode, await respuesta.Content.ReadAsStringAsync(cancelacion));
            }
        }

        // Modelos de texto que la clave puede usar, ordenados del más conveniente al menos
        private async Task<List<string>> ListarModelosAsync(CancellationToken cancelacion)
        {
            using var solicitud = new HttpRequestMessage(HttpMethod.Get,
                EsGemini ? $"{UrlBase}/models?pageSize=200" : $"{UrlBase}/models");

            if (EsGemini) solicitud.Headers.Add("x-goog-api-key", ApiKey);
            else solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);

            using var respuesta = await _http.SendAsync(solicitud, cancelacion);
            var cuerpo = await respuesta.Content.ReadAsStringAsync(cancelacion);
            if (!respuesta.IsSuccessStatusCode)
            {
                _logger.LogWarning("No se pudo listar los modelos de IA ({Estado}): {Detalle}",
                    (int)respuesta.StatusCode, cuerpo.Length > 400 ? cuerpo[..400] : cuerpo);
                return new List<string>();
            }

            using var documento = JsonDocument.Parse(cuerpo);
            var nombres = new List<string>();

            if (EsGemini)
            {
                if (documento.RootElement.TryGetProperty("models", out var modelos)
                    && modelos.ValueKind == JsonValueKind.Array)
                {
                    foreach (var modelo in modelos.EnumerateArray())
                    {
                        var nombre = modelo.TryGetProperty("name", out var n) ? n.GetString() : null;
                        if (string.IsNullOrEmpty(nombre)) continue;

                        // Solo los modelos que admiten generación de texto (generateContent)
                        var generaTexto = modelo.TryGetProperty("supportedGenerationMethods", out var metodos)
                            && metodos.ValueKind == JsonValueKind.Array
                            && metodos.EnumerateArray().Any(x => x.GetString() == "generateContent");
                        if (generaTexto) nombres.Add(nombre.Replace("models/", string.Empty));
                    }
                }

                // Familia Flash (la que tiene nivel gratuito), sin los modelos de imagen, voz o vista previa
                string[] excluidos = { "image", "tts", "live", "audio", "embedding", "native", "robotics", "computer" };
                return nombres
                    .Where(x => x.Contains("gemini") && x.Contains("flash") && !excluidos.Any(x.Contains))
                    .OrderBy(x => x.Contains("preview") || x.Contains("exp"))
                    .ThenBy(x => x.Contains("lite"))
                    .ThenByDescending(x => x, StringComparer.Ordinal)
                    .ToList();
            }

            if (documento.RootElement.TryGetProperty("data", out var datos)
                && datos.ValueKind == JsonValueKind.Array)
            {
                foreach (var modelo in datos.EnumerateArray())
                {
                    if (modelo.TryGetProperty("active", out var activo) && activo.ValueKind == JsonValueKind.False)
                        continue;
                    var id = modelo.TryGetProperty("id", out var i) ? i.GetString() : null;
                    if (!string.IsNullOrEmpty(id)) nombres.Add(id);
                }
            }

            // Se descartan los modelos que no son de conversación (voz, moderación, etc.)
            string[] noConversacion = { "whisper", "guard", "tts", "embed", "playai", "orpheus", "compound", "allam", "vision" };
            // Orden de preferencia entre las familias de modelos de chat
            string[] preferencias = { "gpt-oss-120b", "llama-3.3-70b", "qwen", "kimi", "gpt-oss", "llama", "gemma" };

            return nombres
                .Where(x => !noConversacion.Any(x.ToLowerInvariant().Contains))
                .OrderBy(x =>
                {
                    var posicion = Array.FindIndex(preferencias, p => x.ToLowerInvariant().Contains(p));
                    return posicion < 0 ? preferencias.Length : posicion;
                })
                .ThenBy(x => x, StringComparer.Ordinal)
                .ToList();
        }

        private string ExtraerTexto(string json)
        {
            using var documento = JsonDocument.Parse(json);
            var texto = new StringBuilder();

            if (EsGemini)
            {
                // Gemini: candidates[0].content.parts[].text
                if (documento.RootElement.TryGetProperty("candidates", out var candidatos)
                    && candidatos.ValueKind == JsonValueKind.Array && candidatos.GetArrayLength() > 0
                    && candidatos[0].TryGetProperty("content", out var contenido)
                    && contenido.TryGetProperty("parts", out var partes)
                    && partes.ValueKind == JsonValueKind.Array)
                {
                    foreach (var parte in partes.EnumerateArray())
                    {
                        // Se descartan las partes de razonamiento interno del modelo
                        if (parte.TryGetProperty("thought", out var esRazonamiento)
                            && esRazonamiento.ValueKind == JsonValueKind.True) continue;

                        if (parte.TryGetProperty("text", out var fragmento))
                            texto.Append(fragmento.GetString());
                    }
                }
            }
            else
            {
                // Formato OpenAI: choices[0].message.content
                if (documento.RootElement.TryGetProperty("choices", out var opciones)
                    && opciones.ValueKind == JsonValueKind.Array && opciones.GetArrayLength() > 0
                    && opciones[0].TryGetProperty("message", out var mensaje)
                    && mensaje.TryGetProperty("content", out var contenido)
                    && contenido.ValueKind == JsonValueKind.String)
                {
                    texto.Append(contenido.GetString());
                }
            }

            // Algunos modelos incluyen su razonamiento entre etiquetas <think>: se descarta.
            // Se pide texto plano; si igual llega marcado de Markdown, se quita.
            var limpio = Regex.Replace(texto.ToString(), "<think>.*?</think>", string.Empty, RegexOptions.Singleline);
            return limpio.Replace("**", string.Empty).Trim();
        }

        private static string MensajeDeError(HttpStatusCode? estado) => estado switch
        {
            null =>
                "La clave de IA configurada no tiene modelos disponibles. Informe al administrador.",
            HttpStatusCode.TooManyRequests =>
                "Se alcanzó el límite de consultas del asistente de IA. Espere un minuto e intente nuevamente.",
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.PaymentRequired =>
                "El proveedor de IA rechazó la clave configurada. Informe al administrador.",
            HttpStatusCode.NotFound or HttpStatusCode.BadRequest =>
                "No hay un modelo de IA disponible para la clave configurada. Informe al administrador.",
            _ => "El asistente de IA no está disponible en este momento. Intente nuevamente más tarde."
        };
    }
}
