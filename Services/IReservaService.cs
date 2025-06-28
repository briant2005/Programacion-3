using BBAPP.Data.Models;

namespace BBAPP.Services
{
    public interface IReservaService
    {
        // Obtiene el número total de reservas pendientes para un libro específico.
        Task<int> GetNumeroReservasParaLibroAsync(int libroId);

        // Verifica si un usuario ya tiene una reserva activa (pendiente o lista para recoger) para un libro.
        Task<bool> UsuarioYaTieneReservaActiva(int libroId, string userId);

        // Crea una nueva reserva. Ahora devuelve el modelo Reserva creado.
        Task<Reserva> CrearReservaAsync(int libroId, string userId);

        // Obtiene la reserva activa más antigua para un libro y un usuario, si es el turno de ese usuario.
        // Ahora devuelve el modelo Reserva.
        Task<Reserva> GetReservaActivaParaLibroYUsuarioAsync(int libroId, string userId);

        // Procesa la reclamación de una reserva, convirtiéndola en un préstamo.
        // Devuelve el objeto Prestamo creado o null si falla.
        Task<Prestamo> ReclamarReservaAsync(int reservaId);

        // Métodos de reservas (actualizados para devolver modelos y con el nombre corregido)
        // CAMBIO: Renombrado de GetTodasLasReservasAsync a GetTodasReservasAsync
        Task<IEnumerable<Reserva>> GetTodasReservasAsync();
        Task<IEnumerable<Reserva>> GetReservasPorUsuarioAsync(string userId);
        Task<bool> CancelarReservaAsync(int reservaId);
        Task<Reserva> GetReservaByIdAsync(int reservaId);
    }
}
