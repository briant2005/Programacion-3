using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static BBAPP.Data.ProyectoBibliotecaContext;

namespace BBAPP.Data.Models
{
    public class Reserva
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Libro es obligatorio.")]
        public int LibroId { get; set; }
        [ForeignKey("LibroId")]
        public Libro Libro { get; set; }

        [Required(ErrorMessage = "Usuario es obligatorio.")]
        public string UsuarioId { get; set; }
        [ForeignKey("UsuarioId")]
        public UsuarioAplicacion Usuario { get; set; }

        [Required(ErrorMessage = "Fecha de reserva es obligatoria.")]
        public DateTime FechaReserva { get; set; } = DateTime.UtcNow;

        // Fecha hasta la cual la reserva es válida o cuando se canceló/cumplió
        public DateTime? FechaExpiracion { get; set; }

        // Estado de la reserva: Pendiente, Cumplida, Cancelada
        [Required]
        public EstadoReserva Estado { get; set; } = EstadoReserva.Pendiente;

        // CRÍTICO: Asegúrate de que esta propiedad sea nullable (int?) en tu modelo C#!
        public int? PrestamoId { get; set; }

        public Prestamo Prestamo { get; set; }
    }

    public enum EstadoReserva
    {
        Pendiente,  // Esperando que el libro esté disponible
        Cumplida,   // La reserva se convirtió en un préstamo
        Cancelada,   // La reserva fue cancelada por el usuario o caducó
        ListaParaRecoger
    }
}
