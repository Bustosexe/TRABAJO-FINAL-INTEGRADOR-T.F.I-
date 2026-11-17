using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using StockSuper.Models;
using StockSuper.Models.Enums;
using StockSuper.Services;

namespace StockSuper.Data
{
    // Contexto principal: tablas de Identity + tablas de negocio.
    // Resuelve cuatro responsabilidades transversales:
    //  1. Aislamiento multiempresa (filtros globales por empresa y sucursal).
    //  2. Auditoría automática (usuario y fecha de alta/modificación).
    //  3. Bitácora: cada alta, modificación y eliminación de datos de negocio queda registrada.
    //  4. Integridad: sin borrado en cascada, para no perder historial de stock.
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly IUsuarioActual _usuarioActual;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IUsuarioActual usuarioActual) : base(options)
        {
            _usuarioActual = usuarioActual;
        }

        // Valores del usuario logueado, leídos por los filtros globales en cada consulta
        private int? EmpresaIdActual => _usuarioActual.EmpresaId;
        private int? SucursalIdActual => _usuarioActual.SucursalId;

        public DbSet<Empresa> Empresas => Set<Empresa>();
        public DbSet<Sucursal> Sucursales => Set<Sucursal>();
        public DbSet<Categoria> Categorias => Set<Categoria>();
        public DbSet<Marca> Marcas => Set<Marca>();
        public DbSet<Proveedor> Proveedores => Set<Proveedor>();
        public DbSet<Producto> Productos => Set<Producto>();
        public DbSet<SectorDeposito> SectoresDeposito => Set<SectorDeposito>();
        public DbSet<Pallet> Pallets => Set<Pallet>();
        public DbSet<MovimientoStock> MovimientosStock => Set<MovimientoStock>();

        // Bitácora de auditoría (sin filtro global: cada pantalla la limita según el rol)
        public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigurarUsuario(modelBuilder);
            ConfigurarIndicesUnicos(modelBuilder);
            ConfigurarFiltrosGlobales(modelBuilder);
            ConfigurarAuditoriaYRestricciones(modelBuilder);
            ConfigurarBitacora(modelBuilder);
        }

        // Índices para consultar la bitácora por fecha y por empresa
        private static void ConfigurarBitacora(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RegistroAuditoria>(registro =>
            {
                registro.HasIndex(r => r.FechaUtc);
                registro.HasIndex(r => new { r.EmpresaId, r.Ambito, r.FechaUtc });
            });
        }

        // Vínculo de la cuenta con su empresa y sucursal (sin cascada)
        private static void ConfigurarUsuario(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ApplicationUser>(usuario =>
            {
                usuario.HasOne(u => u.Empresa).WithMany()
                    .HasForeignKey(u => u.EmpresaId)
                    .OnDelete(DeleteBehavior.Restrict);

                usuario.HasOne(u => u.Sucursal).WithMany()
                    .HasForeignKey(u => u.SucursalId)
                    .OnDelete(DeleteBehavior.Restrict);

                usuario.Property(u => u.NombreCuenta).HasMaxLength(150);
            });
        }

        // Los códigos son únicos dentro de cada empresa, no globalmente:
        // dos empresas distintas pueden usar el mismo SKU sin conflicto.
        private static void ConfigurarIndicesUnicos(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Empresa>().HasIndex(e => e.Cuit).IsUnique();

            modelBuilder.Entity<Sucursal>()
                .HasIndex(s => new { s.EmpresaId, s.Codigo }).IsUnique();

            modelBuilder.Entity<Categoria>()
                .HasIndex(c => new { c.EmpresaId, c.Nombre }).IsUnique();

            modelBuilder.Entity<Marca>()
                .HasIndex(m => new { m.EmpresaId, m.Nombre }).IsUnique();

            modelBuilder.Entity<Proveedor>()
                .HasIndex(p => new { p.EmpresaId, p.Cuit }).IsUnique();

            modelBuilder.Entity<Producto>()
                .HasIndex(p => new { p.EmpresaId, p.CodigoSku }).IsUnique();

            modelBuilder.Entity<Pallet>()
                .HasIndex(p => new { p.SucursalId, p.CodigoPallet }).IsUnique();
        }

        // Filtros globales: toda consulta se limita automáticamente a los datos
        // de la empresa del usuario. Reglas:
        //  - Administrador de plataforma (sin EmpresaId): no ve ningún dato de negocio.
        //  - Director (EmpresaId, sin SucursalId): ve todas las sucursales de su empresa.
        //  - Usuario de sucursal (EmpresaId y SucursalId): ve solo su sucursal.
        private void ConfigurarFiltrosGlobales(ModelBuilder modelBuilder)
        {
            // Catálogo: a nivel empresa
            modelBuilder.Entity<Categoria>().HasQueryFilter(e => e.EmpresaId == EmpresaIdActual);
            modelBuilder.Entity<Marca>().HasQueryFilter(e => e.EmpresaId == EmpresaIdActual);
            modelBuilder.Entity<Proveedor>().HasQueryFilter(e => e.EmpresaId == EmpresaIdActual);
            modelBuilder.Entity<Producto>().HasQueryFilter(e => e.EmpresaId == EmpresaIdActual);

            // La sucursal en sí: el usuario de sucursal solo ve la suya
            modelBuilder.Entity<Sucursal>().HasQueryFilter(e =>
                e.EmpresaId == EmpresaIdActual &&
                (SucursalIdActual == null || e.Id == SucursalIdActual));

            // Depósito y stock: a nivel sucursal
            modelBuilder.Entity<SectorDeposito>().HasQueryFilter(e =>
                e.EmpresaId == EmpresaIdActual &&
                (SucursalIdActual == null || e.SucursalId == SucursalIdActual));

            modelBuilder.Entity<Pallet>().HasQueryFilter(e =>
                e.EmpresaId == EmpresaIdActual &&
                (SucursalIdActual == null || e.SucursalId == SucursalIdActual));

            modelBuilder.Entity<MovimientoStock>().HasQueryFilter(e =>
                e.EmpresaId == EmpresaIdActual &&
                (SucursalIdActual == null || e.SucursalId == SucursalIdActual));
        }

        // FK de auditoría hacia AspNetUsers en todas las tablas de negocio y
        // eliminación restringida en todas las relaciones propias.
        private static void ConfigurarAuditoriaYRestricciones(ModelBuilder modelBuilder)
        {
            var tiposAuditables = modelBuilder.Model.GetEntityTypes()
                .Where(t => t.ClrType.IsSubclassOf(typeof(AuditableEntity)))
                .Select(t => t.ClrType)
                .ToList();

            foreach (var tipo in tiposAuditables)
            {
                var entidad = modelBuilder.Entity(tipo);

                entidad.HasOne(typeof(ApplicationUser)).WithMany()
                    .HasForeignKey(nameof(AuditableEntity.UsuarioCreacionId))
                    .OnDelete(DeleteBehavior.Restrict);

                entidad.HasOne(typeof(ApplicationUser)).WithMany()
                    .HasForeignKey(nameof(AuditableEntity.UsuarioModificacionId))
                    .OnDelete(DeleteBehavior.Restrict);
            }

            // Nunca se borra en cascada: eliminar un padre con hijos debe fallar
            foreach (var tipo in tiposAuditables)
            {
                foreach (var fk in modelBuilder.Entity(tipo).Metadata.GetForeignKeys())
                    fk.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }

        // Auditoría automática: ningún controlador completa estos campos a mano. Además, cada
        // alta, modificación y eliminación de una entidad de negocio queda en la bitácora,
        // en la misma transacción que el cambio: si falla uno, no se guarda ninguno.
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            AplicarAuditoria();
            var pendientes = RecolectarBitacora();
            if (pendientes.Count == 0) return base.SaveChanges(acceptAllChangesOnSuccess);

            using var transaccion = Database.CurrentTransaction == null ? Database.BeginTransaction() : null;
            var filas = base.SaveChanges(acceptAllChangesOnSuccess);

            // La bitácora se arma después de guardar: recién ahí existen los Id generados
            var registros = ConstruirBitacora(pendientes);
            if (registros.Count > 0)
            {
                RegistrosAuditoria.AddRange(registros);
                base.SaveChanges(acceptAllChangesOnSuccess);
            }

            transaccion?.Commit();
            return filas;
        }

        public override async Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            AplicarAuditoria();
            var pendientes = RecolectarBitacora();
            if (pendientes.Count == 0)
                return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

            await using var transaccion = Database.CurrentTransaction == null
                ? await Database.BeginTransactionAsync(cancellationToken) : null;
            var filas = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

            var registros = ConstruirBitacora(pendientes);
            if (registros.Count > 0)
            {
                RegistrosAuditoria.AddRange(registros);
                await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            }

            if (transaccion != null) await transaccion.CommitAsync(cancellationToken);
            return filas;
        }

        // Cambio de una entidad detectado antes de guardar
        private sealed record CambioPendiente(AuditableEntity Entidad, EntityState Estado, string? Cambios);

        // Campos de auditoría y técnicos: no se muestran como cambios del usuario
        private static readonly HashSet<string> PropiedadesOmitidas = new()
        {
            nameof(AuditableEntity.UsuarioCreacionId), nameof(AuditableEntity.FechaCreacion),
            nameof(AuditableEntity.UsuarioModificacionId), nameof(AuditableEntity.FechaModificacion),
            "RowVersion", "RemitoRuta"
        };

        private List<CambioPendiente> RecolectarBitacora()
        {
            var lista = new List<CambioPendiente>();

            foreach (var entrada in ChangeTracker.Entries<AuditableEntity>())
            {
                if (entrada.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                    continue;

                // Los movimientos de stock ya son su propio historial inmutable
                if (entrada.Entity is MovimientoStock) continue;

                string? cambios = null;
                if (entrada.State == EntityState.Modified)
                {
                    cambios = DescribirCambios(entrada);
                    if (cambios == null) continue;   // solo cambió algo técnico: no se registra
                }

                lista.Add(new CambioPendiente(entrada.Entity, entrada.State, cambios));
            }

            return lista;
        }

        // "Campo: de X a Y" por cada propiedad que cambió realmente
        private static string? DescribirCambios(EntityEntry entrada)
        {
            var partes = new List<string>();

            foreach (var propiedad in entrada.Properties)
            {
                var nombre = propiedad.Metadata.Name;
                if (!propiedad.IsModified || PropiedadesOmitidas.Contains(nombre)) continue;

                // La cantidad de un pallet cambia con cada movimiento, que ya consta en su historial
                if (entrada.Entity is Pallet && nombre == nameof(Pallet.CantidadActual)) continue;

                if (Equals(propiedad.OriginalValue, propiedad.CurrentValue)) continue;

                partes.Add($"{EtiquetasAuditoria.Campo(nombre)}: de {EtiquetasAuditoria.Valor(propiedad.OriginalValue)} " +
                           $"a {EtiquetasAuditoria.Valor(propiedad.CurrentValue)}");
            }

            return partes.Count == 0 ? null : string.Join("; ", partes);
        }

        private List<RegistroAuditoria> ConstruirBitacora(List<CambioPendiente> pendientes)
        {
            var ahora = DateTime.UtcNow;
            var cuenta = _usuarioActual.NombreCuenta ?? "Sistema";
            var registros = new List<RegistroAuditoria>();

            foreach (var cambio in pendientes)
            {
                var entidad = cambio.Entidad;
                var nombreClase = entidad.GetType().Name;

                int? empresaId = entidad switch
                {
                    Empresa e => e.Id,
                    EmpresaEntity ee => ee.EmpresaId,
                    _ => null
                };
                int? sucursalId = entidad switch
                {
                    Sucursal s => s.Id,
                    SucursalEntity se => se.SucursalId,
                    _ => null
                };

                var identificacion = EtiquetasAuditoria.Identificar(entidad);
                var detalle = cambio.Cambios == null
                    ? identificacion
                    : identificacion.Length > 0 ? identificacion + " - " + cambio.Cambios : cambio.Cambios;
                if (detalle.Length > 500) detalle = detalle[..497] + "...";

                registros.Add(new RegistroAuditoria
                {
                    FechaUtc = ahora,
                    // Empresas y sucursales las administra la plataforma; el resto es de la empresa
                    Ambito = entidad is Empresa or Sucursal ? AmbitoAuditoria.Plataforma : AmbitoAuditoria.Negocio,
                    Accion = cambio.Estado switch
                    {
                        EntityState.Added => "Alta",
                        EntityState.Deleted => "Eliminación",
                        _ => "Modificación"
                    },
                    Entidad = EtiquetasAuditoria.Entidad(nombreClase),
                    EntidadId = entidad.GetType().GetProperty("Id")?.GetValue(entidad)?.ToString(),
                    Detalle = detalle.Length == 0 ? null : detalle,
                    UsuarioId = _usuarioActual.UsuarioId,
                    UsuarioNombre = cuenta.Length > 150 ? cuenta[..150] : cuenta,
                    EmpresaId = empresaId,
                    SucursalId = sucursalId
                });
            }

            return registros;
        }

        private void AplicarAuditoria()
        {
            // Se guarda en UTC; en las vistas se convierte a hora local de Argentina
            var ahora = DateTime.UtcNow;
            var usuarioId = _usuarioActual.UsuarioId;

            foreach (var entrada in ChangeTracker.Entries<AuditableEntity>())
            {
                switch (entrada.State)
                {
                    case EntityState.Added:
                        entrada.Entity.FechaCreacion = ahora;
                        entrada.Entity.UsuarioCreacionId = usuarioId;

                        // Empresa y sucursal salen de la sesión, nunca del formulario.
                        // Solo se completan si no vienen asignadas (el administrador de
                        // plataforma las asigna explícitamente al crear sucursales).
                        if (entrada.Entity is EmpresaEntity conEmpresa
                            && conEmpresa.EmpresaId == 0
                            && _usuarioActual.EmpresaId.HasValue)
                        {
                            conEmpresa.EmpresaId = _usuarioActual.EmpresaId.Value;
                        }

                        if (entrada.Entity is SucursalEntity conSucursal
                            && conSucursal.SucursalId == 0
                            && _usuarioActual.SucursalId.HasValue)
                        {
                            conSucursal.SucursalId = _usuarioActual.SucursalId.Value;
                        }
                        break;

                    case EntityState.Modified:
                        // Los datos de alta no se pueden alterar en una edición
                        entrada.Property(e => e.UsuarioCreacionId).IsModified = false;
                        entrada.Property(e => e.FechaCreacion).IsModified = false;

                        entrada.Entity.FechaModificacion = ahora;
                        entrada.Entity.UsuarioModificacionId = usuarioId;
                        break;
                }
            }
        }
    }
}