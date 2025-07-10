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

        // Obtiene solo los préstamos pendientes de aprobación
        Task<IEnumerable<PrestamoDto>> GetPrestamosPendientesAprobacionAsync();

        // Realiza un nuevo préstamo (ahora con estado PendienteAprobacion)
        Task<Prestamo> RealizarPrestamoAsync(Prestamo nuevoPrestamo);

        // Procesa la devolución de un préstamo, devolviendo el modelo Prestamo actualizado
        Task<Prestamo> ProcesarDevolucionAsync(int prestamoId);

        // Elimina un préstamo, devolviendo un booleano de éxito
        Task<bool> EliminarPrestamoAsync(int id);

        // Métodos para aprobar y denegar solicitudes de préstamo
        Task<Prestamo> AprobarPrestamoAsync(int prestamoId);
        Task<Prestamo> DenegarPrestamoAsync(int prestamoId);

        // Métodos auxiliares para obtener libros y usuarios (devuelven los modelos completos)
        Task<IEnumerable<Libro>> GetLibrosDisponiblesAsync();
        Task<Libro> GetLibroByIdAsync(int libroId);
        Task<UsuarioAplicacion> GetUsuarioByIdAsync(string userId);
        Task<IEnumerable<UsuarioAplicacion>> GetTodosUsuariosAsync();
        Task<bool> PuedeUsuarioPedirPrestadoLibro(string usuarioId);
        Task<bool> TieneLibroCopiasDisponibles(int libroId);

        // Nuevos métodos para verificar razones específicas de ineligibilidad
        Task<bool> HasReachedLoanLimitAsync(string userId);
        Task<bool> HasOverdueLoansAsync(string userId);

        // Nuevo método para calcular y registrar multas
        Task<Multa> CalcularYRegistrarMultaAsync(int prestamoId);

        // Nueva propiedad para exponer el límite de libros por usuario
        int MaxLibrosPorUsuario { get; }
    }
}
