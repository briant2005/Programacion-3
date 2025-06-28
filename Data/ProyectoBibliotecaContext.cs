using Microsoft.EntityFrameworkCore;
using BBAPP.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using static BBAPP.Data.ProyectoBibliotecaContext;

namespace BBAPP.Data
{
    // Cambiamos de DbContext a IdentityDbContext<UsuarioAplicacion>
    public class ProyectoBibliotecaContext : IdentityDbContext<UsuarioAplicacion>
    {
        public ProyectoBibliotecaContext(DbContextOptions<ProyectoBibliotecaContext> options)
            : base(options)
        {
        }

        public DbSet<Libro> Libros { get; set; }
        public DbSet<Prestamo> Prestamos { get; set; }
        public DbSet<Reserva> Reservas { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // MUY IMPORTANTE: Llama al método base para que Identity configure sus tablas

            // Configurar relaciones para Prestamo
            builder.Entity<Prestamo>()
                .HasOne(p => p.Libro)
                .WithMany(b => b.Prestamos)
                .HasForeignKey(p => p.LibroId);

            builder.Entity<Prestamo>()
                .HasOne(p => p.Usuario)
                .WithMany(u => u.Prestamos)
                .HasForeignKey(p => p.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configurar relaciones para Reserva
            builder.Entity<Reserva>()
                .HasOne(r => r.Libro)
                .WithMany(l => l.Reservas)
                .HasForeignKey(r => r.LibroId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Reserva>()
                .HasOne(r => r.Usuario)
                .WithMany(u => u.Reservas)
                .HasForeignKey(r => r.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // La relación de Reserva a Prestamo ha sido eliminada por tu solicitud.
            // Esto significa que PrestamoId ya no será una clave foránea en la tabla Reservas.
            // Si la columna PrestamoId aún existe en tu modelo Reserva, se tratará como una columna regular.
            // Si quieres eliminarla completamente, debes quitar la propiedad 'public int? PrestamoId { get; set; }'
            // de tu archivo BBAPP.Data.Models/Reserva.cs.

            // Sembrar roles (Opcional, manejado en Program.cs también)
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = Guid.NewGuid().ToString(), Name = "Admin", NormalizedName = "ADMIN" },
                new IdentityRole { Id = Guid.NewGuid().ToString(), Name = "Bibliotecario", NormalizedName = "BIBLIOTECARIO" },
                new IdentityRole { Id = Guid.NewGuid().ToString(), Name = "Usuario", NormalizedName = "USUARIO" }
            );
        }
    }
}
