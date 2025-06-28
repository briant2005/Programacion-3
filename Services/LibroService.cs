using BBAPP.Data;
using BBAPP.Data.Models;
using BBAPP.DTOs;
using Microsoft.EntityFrameworkCore;
using static BBAPP.Data.ProyectoBibliotecaContext;

namespace BBAPP.Services
{
    public class LibroService : ILibroService
    {
        private readonly ProyectoBibliotecaContext _context;

        public LibroService(ProyectoBibliotecaContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Libro>> GetTodosLibrosAsync()
        {
            return await _context.Libros.ToListAsync();
        }

        public async Task<Libro> GetLibroByIdAsync(int id)
        {
            return await _context.Libros.FindAsync(id);
        }

        public async Task<Libro> AddLibroAsync(Libro libro)
        {
            // Asegurarse de que las copias disponibles no excedan el total de copias al añadir
            if (libro.CopiasDisponibles > libro.TotalCopias)
            {
                libro.CopiasDisponibles = libro.TotalCopias;
            }
            _context.Libros.Add(libro);
            await _context.SaveChangesAsync();
            return libro;
        }

        public async Task<Libro> UpdateLibroAsync(Libro updatedLibro) // Renombrado para claridad
        {
            var existingLibro = await _context.Libros.FindAsync(updatedLibro.id);
            if (existingLibro == null)
            {
                return null; // O lanzar una excepción
            }

            // Actualizar propiedades del modelo existente
            existingLibro.Titulo = updatedLibro.Titulo;
            existingLibro.Autor = updatedLibro.Autor;
            existingLibro.AnioPublicacion = updatedLibro.AnioPublicacion;
            existingLibro.Genero = updatedLibro.Genero;
            existingLibro.TotalCopias = updatedLibro.TotalCopias;
            // Asegurarse de que las copias disponibles no excedan el total de copias al actualizar
            if (updatedLibro.CopiasDisponibles > updatedLibro.TotalCopias)
            {
                existingLibro.CopiasDisponibles = updatedLibro.TotalCopias;
            }
            else
            {
                existingLibro.CopiasDisponibles = updatedLibro.CopiasDisponibles;
            }

            _context.Libros.Update(existingLibro);
            await _context.SaveChangesAsync();
            return existingLibro;
        }

        public async Task<bool> DeleteLibroAsync(int id)
        {
            var libro = await _context.Libros.FindAsync(id);
            if (libro == null)
            {
                return false;
            }
            _context.Libros.Remove(libro);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
