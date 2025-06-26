using Microsoft.EntityFrameworkCore;
using BBAPP.Data.Models;

namespace BBAPP.Data
{
    public class ProyectoBibliotecaContext : DbContext
    {
        public ProyectoBibliotecaContext(DbContextOptions<ProyectoBibliotecaContext> options)
            : base(options) { }

        public DbSet<Libros> Libros { get; set; }
        public DbSet<Usuarios> Usuarios { get; set; }
        // … añade aquí todos tus DbSet<…> para cada tabla
    }

    // Ejemplo de entidad para la tabla Libros
    public class Libros
    {
        public int Id_Libro { get; set; }
        public string Titulo { get; set; }
        public string Autor { get; set; }
        public int Stock { get; set; }
        // … el resto de columnas
    }
}
