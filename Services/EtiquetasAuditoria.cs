using System.Globalization;
using System.Text.RegularExpressions;
using StockSuper.Models.Enums;

namespace StockSuper.Services
{
    // Textos legibles para la bitácora: nombres de entidades y campos, valores y
    // la identificación del registro afectado (nombre, código, razón social).
    public static class EtiquetasAuditoria
    {
        private static readonly CultureInfo Cultura = new("es-AR");

        private static readonly string[] CamposIdentificacion =
            { "RazonSocial", "NombreSector", "CodigoPallet", "Nombre", "CodigoSku" };

        public static string Ambito(AmbitoAuditoria ambito) => ambito switch
        {
            AmbitoAuditoria.Plataforma => "Plataforma",
            AmbitoAuditoria.Acceso => "Acceso",
            AmbitoAuditoria.Negocio => "Operaciones",
            _ => ambito.ToString()
        };

        public static string Entidad(string nombreClase) => nombreClase switch
        {
            "SectorDeposito" => "Sector de depósito",
            "Categoria" => "Categoría",
            _ => nombreClase
        };

        // "PrecioVenta" -> "Precio venta"
        public static string Campo(string nombre)
        {
            var separado = Regex.Replace(nombre, "(?<!^)([A-Z])", " $1").ToLowerInvariant();
            return char.ToUpperInvariant(separado[0]) + separado[1..];
        }

        public static string Valor(object? valor)
        {
            var texto = valor switch
            {
                null => "vacío",
                bool b => b ? "Sí" : "No",
                DateTime f => f.TimeOfDay == TimeSpan.Zero
                    ? f.ToString("dd/MM/yyyy", Cultura)
                    : f.ToString("dd/MM/yyyy HH:mm", Cultura),
                decimal d => d.ToString("N2", Cultura),
                IFormattable formateable => formateable.ToString(null, Cultura),
                _ => valor.ToString() ?? string.Empty
            };

            if (texto.Length == 0) texto = "vacío";
            return texto.Length > 60 ? texto[..57] + "..." : texto;
        }

        // Nombre, razón social o código que identifica al registro afectado
        public static string Identificar(object entidad)
        {
            foreach (var campo in CamposIdentificacion)
            {
                var valor = entidad.GetType().GetProperty(campo)?.GetValue(entidad) as string;
                if (!string.IsNullOrWhiteSpace(valor)) return valor.Trim();
            }
            return string.Empty;
        }
    }
}
