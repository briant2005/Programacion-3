using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using static BBAPP.Data.ProyectoBibliotecaContext;

namespace BBAPP.Data.Models
{
    public class Prestamo
    {
        [Key] // Marca Id como la clave primaria
        public int Id { get; set; }

        [Required(ErrorMessage = "Libro es obligatorio.")]
        public int LibroId { get; set; }
        [ForeignKey("LibroId")]
        public Libro Libro { get; set; } // ¡ASEGÚRATE DE QUE ESTA PROPIEDAD ESTÉ PRESENTE!

        [Required(ErrorMessage = "Usuario es obligatorio.")]
        public string UsuarioId { get; set; }
        [ForeignKey("UsuarioId")]
        public UsuarioAplicacion Usuario { get; set; }

        [Required(ErrorMessage = "La fecha de préstamo es obligatoria.")]
        public DateTime FechaPrestamo { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "La fecha de vencimiento es obligatoria.")]
        public DateTime FechaVencimiento { get; set; }

        public DateTime? FechaDevolucion { get; set; }

        [Required(ErrorMessage = "El estado del préstamo es obligatorio.")]
        public EstadoPrestamo Estado { get; set; } = EstadoPrestamo.Activo;
    }

    public enum EstadoPrestamo
    {
        Activo,
        Devuelto,
        Atrasado,
        Cancelado
    }
}
