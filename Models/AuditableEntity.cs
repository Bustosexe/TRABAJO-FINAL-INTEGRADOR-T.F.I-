namespace StockSuper.Models
{
    // Clase base de auditoría: toda entidad de negocio (Producto, Lote,
    // Proveedor, etc.) hereda estos campos para trazar qué usuario
    // creó o modificó cada registro y en qué momento.
    public abstract class AuditableEntity
    {
        // FK a AspNetUsers: usuario que dio de alta el registro
        public string? UsuarioCreacionId { get; set; }

        // Fecha y hora de alta del registro (obligatoria)
        public DateTime FechaCreacion { get; set; }

        // FK a AspNetUsers: último usuario que modificó el registro
        public string? UsuarioModificacionId { get; set; }

        // Fecha y hora de la última modificación (nula hasta que se edite)
        public DateTime? FechaModificacion { get; set; }
    }
}