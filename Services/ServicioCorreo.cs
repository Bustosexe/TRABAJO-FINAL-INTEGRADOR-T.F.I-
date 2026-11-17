using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace StockSuper.Services
{
    // Envío de correos por SMTP (Gmail con contraseña de aplicación). Usuario y clave
    // se leen de la configuración (User Secrets / variables de entorno), nunca del código.
    // Es reutilizable: el reporte de stock lo usa con adjunto PDF y, más adelante,
    // los pedidos de reposición a proveedores redactados con IA.
    public class ServicioCorreo
    {
        private readonly IConfiguration _config;

        public ServicioCorreo(IConfiguration config) => _config = config;

        // Falso si todavía no se cargaron las credenciales: la pantalla avisa en lugar de fallar
        public bool Configurado =>
            !string.IsNullOrWhiteSpace(_config["Correo:Usuario"]) &&
            !string.IsNullOrWhiteSpace(_config["Correo:Password"]);

        public async Task EnviarAsync(
            string destino, string asunto, string cuerpo,
            string? nombreAdjunto = null, byte[]? adjunto = null)
        {
            var usuario = _config["Correo:Usuario"]!;
            var host = _config["Correo:Host"] ?? "smtp.gmail.com";
            var puerto = int.TryParse(_config["Correo:Puerto"], out var p) ? p : 587;

            var mensaje = new MimeMessage();
            mensaje.From.Add(new MailboxAddress(_config["Correo:NombreRemitente"] ?? "StockSuper", usuario));
            mensaje.To.Add(MailboxAddress.Parse(destino));
            mensaje.Subject = asunto;

            var cuerpoMensaje = new BodyBuilder { TextBody = cuerpo };
            if (adjunto != null && nombreAdjunto != null)
                cuerpoMensaje.Attachments.Add(nombreAdjunto, adjunto, new ContentType("application", "pdf"));
            mensaje.Body = cuerpoMensaje.ToMessageBody();

            using var cliente = new SmtpClient();
            await cliente.ConnectAsync(host, puerto, SecureSocketOptions.StartTls);
            await cliente.AuthenticateAsync(usuario, _config["Correo:Password"]);
            await cliente.SendAsync(mensaje);
            await cliente.DisconnectAsync(true);
        }
    }
}