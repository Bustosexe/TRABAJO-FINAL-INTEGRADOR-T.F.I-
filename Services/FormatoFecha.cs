namespace StockSuper.Services
{
    // Las fechas se guardan en UTC; esta extensión las muestra en hora de Argentina
    // (UTC-3, sin horario de verano).
    public static class FormatoFecha
    {
        public static DateTime AHoraArgentina(this DateTime fechaUtc) => fechaUtc.AddHours(-3);

        // Fecha de hoy en Argentina. No depende de la zona horaria del servidor: en un
        // hosting configurado en UTC, DateTime.Today cambiaría de día a las 21:00 de Argentina
        // y correría un día los cálculos de vencimiento.
        public static DateTime HoyArgentina => DateTime.UtcNow.AHoraArgentina().Date;
    }
}