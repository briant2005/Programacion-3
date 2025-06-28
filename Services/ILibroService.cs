using BBAPP.Data.Models;
using BBAPP.DTOs;

namespace BBAPP.Services
{
    public interface ILibroService
    {
        Task<IEnumerable<Libro>> GetTodosLibrosAsync();
        Task<Libro> GetLibroByIdAsync(int id);
        Task<Libro> AddLibroAsync(Libro libro);
        Task<Libro> UpdateLibroAsync(Libro libro); // Actualizado para tomar y devolver el modelo Libro
        Task<bool> DeleteLibroAsync(int id);
    }
}
