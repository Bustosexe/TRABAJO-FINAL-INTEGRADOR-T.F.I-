namespace StockSuper.Models.ViewModels
{
    // Datos de auditoría de un registro: quién lo creó y quién lo modificó por última vez
    public class PieAuditoriaViewModel
    {
        public string CreadoPor { get; set; } = "Sistema";
        public DateTime FechaCreacion { get; set; }
        public string? ModificadoPor { get; set; }
        public DateTime? FechaModificacion { get; set; }
    }
}