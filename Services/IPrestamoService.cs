using BBAPP.Data.Models;
using BBAPP.DTOs;
using static BBAPP.Data.ProyectoBibliotecaContext;

namespace BBAPP.Services
{
    public interface IPrestamoService
    {
        // Obtiene todos los préstamos como DTOs
        Task<IEnumerable<PrestamoDto>> GetTodosPrestamosAsync();

        // Obtiene un préstamo específico por ID como DTO
        Task<PrestamoDto> GetPrestamoByIdAsync(int id);

        // Obtiene los préstamos de un usuario específico como DTOs
        Task<IEnumerable<PrestamoDto>> ObtenerPrestamosDeUsuarioAsync(string userId);

        // Realiza un nuevo préstamo, devolviendo el modelo Prestamo completo
        Task<Prestamo> RealizarPrestamoAsync(Prestamo nuevoPrestamo);

        // Procesa la devolución de un préstamo, devolviendo el modelo Prestamo actualizado
        Task<Prestamo> ProcesarDevolucionAsync(int prestamoId);

        // Elimina un préstamo, devolviendo un booleano de éxito
        Task<bool> EliminarPrestamoAsync(int id);

        // Métodos auxiliares para obtener libros y usuarios (devuelven los modelos completos)
        Task<IEnumerable<Libro>> GetLibrosDisponiblesAsync();
        Task<Libro> GetLibroByIdAsync(int libroId);
        Task<UsuarioAplicacion> GetUsuarioByIdAsync(string userId);
        Task<IEnumerable<UsuarioAplicacion>> GetTodosUsuariosAsync();
        Task<bool> PuedeUsuarioPedirPrestadoLibro(string usuarioId); // Agregado si falta
        Task<bool> TieneLibroCopiasDisponibles(int libroId); // Agregado si falta
    }
}
