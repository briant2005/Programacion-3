using Microsoft.AspNetCore.Identity;

namespace BBAPP.Data.Models
{
    public class UsuarioAplicacion : IdentityUser
    {
        // Añade cualquier propiedad personalizada para tu usuario aquí, por ejemplo:
        public string Nombre { get; set; }


        public string Apellido { get; set; }

        // Propiedad de navegación para préstamos
        public ICollection<Prestamo> Prestamos { get; set; } = new List<Prestamo>();
        public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>(); // ¡AÑADIDA ESTA LÍNEA!

    }
}
