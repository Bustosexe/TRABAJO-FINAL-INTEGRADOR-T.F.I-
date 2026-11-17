namespace StockSuper.Models
{
    // Base de toda entidad que pertenece a una empresa cliente.
    // El DbContext aplica un filtro global por EmpresaId, de modo que
    // el stock de una empresa nunca se mezcla con el de otra.
    public abstract class EmpresaEntity : AuditableEntity
    {
        // Se asigna en el servidor desde el claim del usuario, nunca desde el formulario
        public int EmpresaId { get; set; }
        public Empresa? Empresa { get; set; }
    }
}