namespace StockSuper.Models
{
    // Base de toda entidad que pertenece a una sucursal concreta (pallets,
    // sectores de depósito y movimientos). Un usuario de sucursal solo
    // ve registros de su propia sucursal.
    public abstract class SucursalEntity : EmpresaEntity
    {
        public int SucursalId { get; set; }
        public Sucursal? Sucursal { get; set; }
    }
}