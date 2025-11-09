using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProyectoFinalCalidad.Models;
using System.Diagnostics.Contracts;

namespace ProyectoFinalCalidad.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets de tus entidades personalizadas
        public DbSet<Cargo> Cargos { get; set; }
        public DbSet<Empleado> Empleados { get; set; }
        public DbSet<ZonaCobertura> ZonaCoberturas { get; set; }
        public DbSet<PlanServicio> PlanServicios { get; set; }
        public DbSet<Equipo> Equipos { get; set; }
        public DbSet<Contrato> Contratos { get; set; }
        public DbSet<ContratoEquipo> ContratoEquipos { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Pago> Pagos { get; set; }
        public DbSet<PedidoInstalacion> PedidosInstalacion { get; set; }
        public DbSet<EquipoUnidad> EquiposUnidades { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Cargo>(entity =>
            {
                entity.ToTable("Cargo");
                entity.HasKey(c => c.cargo_id);

                entity.Property(c => c.titulo_cargo)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.HasMany(c => c.Empleados)
                      .WithOne(e => e.Cargo)
                      .HasForeignKey(e => e.cargo_id);
            });

            modelBuilder.Entity<Empleado>(entity =>
            {
                entity.ToTable("Empleado");
                entity.HasKey(e => e.empleado_id);

                entity.Property(e => e.cargo_id).HasColumnName("cargo_id");
                entity.Property(e => e.nombres).IsRequired().HasMaxLength(100);
                entity.Property(e => e.dni).IsRequired().HasMaxLength(8);
                entity.Property(e => e.telefono).HasMaxLength(15);
                entity.Property(e => e.correo).HasMaxLength(100);
                entity.Property(e => e.password).IsRequired();
                entity.Property(e => e.estado).IsRequired().HasMaxLength(20);
                entity.Property(e => e.fecha_inicio);
                entity.Property(e => e.fecha_fin);
            });

            modelBuilder.Entity<PedidoInstalacion>(entity =>
            {
                entity.ToTable("PedidoInstalacion");
                entity.HasKey(p => p.PedidoId);

                entity.Property(p => p.PedidoId).HasColumnName("pedido_id");
                entity.Property(p => p.ContratoId).HasColumnName("contrato_id");
                entity.Property(p => p.EmpleadoId).HasColumnName("empleado_id");
                entity.Property(p => p.FechaInstalacion).HasColumnName("fecha_instalacion");
                entity.Property(p => p.EstadoInstalacion).HasColumnName("estado_instalacion");
                entity.Property(p => p.Observaciones).HasColumnName("observaciones");
            });

            modelBuilder.Entity<Cliente>().ToTable("Cliente");
            modelBuilder.Entity<ZonaCobertura>().ToTable("Zona_cobertura");
            modelBuilder.Entity<PlanServicio>().ToTable("Plan_servicio");
            modelBuilder.Entity<Equipo>().ToTable("Equipo");
            modelBuilder.Entity<Contrato>().ToTable("Contrato");
            modelBuilder.Entity<ContratoEquipo>().ToTable("ContratoEquipo");
            modelBuilder.Entity<Pago>().ToTable("Pago");
            modelBuilder.Entity<EquipoUnidad>().ToTable("EquipoUnidad");
        }

        // M�todo auxiliar para pagos pendientes
        public IQueryable<Pago> GetPagosPendientesProximosTresDias()
        {
            var hoy = DateTime.Now.Date;
            var limite = hoy.AddDays(3);
            return Pagos
                .Where(p => p.EstadoPago == "pendiente" &&
                            p.FechaDeVencimiento >= hoy &&
                            p.FechaDeVencimiento <= limite);
        }
    }
}
