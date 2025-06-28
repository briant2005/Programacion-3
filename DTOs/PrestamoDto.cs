using BBAPP.Data.Models;

namespace BBAPP.DTOs
{
    public class PrestamoDto
    {
        public int Id { get; set; }
        public int LibroId { get; set; }
        public string UsuarioId { get; set; }
        public DateTime FechaPrestamo { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public DateTime? FechaDevolucion { get; set; }
        public EstadoPrestamo Estado { get; set; }

        // Propiedades para mostrar información relacionada
        public string TituloLibro { get; set; } // Renombrado de LibroTitulo a TituloLibro para coincidir con tu snippet
        public string LibroAutor { get; set; }
        public string UsuarioNombreCompleto { get; set; }
    }
}
