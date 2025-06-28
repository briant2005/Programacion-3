using BBAPP.Data;
using BBAPP.Data.Models;
using BBAPP.DTOs; // Necesario para DTOs
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using System;
using static BBAPP.Data.ProyectoBibliotecaContext;

namespace BBAPP.Services
{
    public class PrestamoService : IPrestamoService
    {
        private readonly ProyectoBibliotecaContext _context;
        private readonly UserManager<UsuarioAplicacion> _userManager;

        public PrestamoService(ProyectoBibliotecaContext context, UserManager<UsuarioAplicacion> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Método auxiliar para construir una consulta base para préstamos, incluyendo Libro y Usuario
        private IQueryable<Prestamo> GetPrestamosBaseQuery()
        {
            return _context.Prestamos
                           .Include(p => p.Libro)
                           .Include(p => p.Usuario);
        }

        // Método auxiliar para mapear un modelo Prestamo a un PrestamoDto
        private PrestamoDto MapToPrestamoDto(Prestamo prestamo)
        {
            if (prestamo == null) return null;

            return new PrestamoDto
            {
                Id = prestamo.Id,
                LibroId = prestamo.LibroId,
                UsuarioId = prestamo.UsuarioId,
                FechaPrestamo = prestamo.FechaPrestamo,
                FechaVencimiento = prestamo.FechaVencimiento,
                FechaDevolucion = prestamo.FechaDevolucion,
                Estado = prestamo.Estado,
                TituloLibro = prestamo.Libro?.Titulo,
                LibroAutor = prestamo.Libro?.Autor,
                UsuarioNombreCompleto = $"{prestamo.Usuario?.Nombre} {prestamo.Usuario?.Apellido}".Trim()
            };
        }

        // Implementación del método para obtener todos los préstamos y mapearlos a DTOs
        public async Task<IEnumerable<PrestamoDto>> GetTodosPrestamosAsync()
        {
            return (await GetPrestamosBaseQuery()
                               .OrderByDescending(p => p.FechaPrestamo)
                               .ToListAsync())
                       .Select(p => MapToPrestamoDto(p));
        }

        // Implementación del método para obtener un préstamo por ID y mapearlo a DTO
        public async Task<PrestamoDto> GetPrestamoByIdAsync(int id)
        {
            var prestamo = await GetPrestamosBaseQuery().FirstOrDefaultAsync(p => p.Id == id);
            return MapToPrestamoDto(prestamo);
        }

        // Implementación del método para obtener préstamos de un usuario específico
        public async Task<IEnumerable<PrestamoDto>> ObtenerPrestamosDeUsuarioAsync(string usuarioId)
        {
            return (await GetPrestamosBaseQuery()
                               .Where(p => p.UsuarioId == usuarioId)
                               .OrderByDescending(p => p.FechaPrestamo)
                               .ToListAsync())
                       .Select(p => MapToPrestamoDto(p));
        }

        // Implementación para realizar un préstamo
        public async Task<Prestamo> RealizarPrestamoAsync(Prestamo nuevoPrestamo)
        {
            var libro = await _context.Libros.FindAsync(nuevoPrestamo.LibroId);
            if (libro == null)
            {
                throw new InvalidOperationException("El libro no fue encontrado.");
            }
            if (libro.CopiasDisponibles <= 0)
            {
                throw new InvalidOperationException("El libro no tiene copias disponibles para préstamo.");
            }

            if (!await PuedeUsuarioPedirPrestadoLibro(nuevoPrestamo.UsuarioId))
            {
                throw new InvalidOperationException("El usuario ha alcanzado su límite de préstamos o no cumple los requisitos.");
            }

            libro.CopiasDisponibles--;
            _context.Libros.Update(libro);

            nuevoPrestamo.FechaPrestamo = DateTime.UtcNow;
            nuevoPrestamo.Estado = EstadoPrestamo.Activo;
            nuevoPrestamo.FechaDevolucion = null;

            _context.Prestamos.Add(nuevoPrestamo);
            await _context.SaveChangesAsync();
            return nuevoPrestamo;
        }

        // Implementación para procesar la devolución de un préstamo
        public async Task<Prestamo> ProcesarDevolucionAsync(int prestamoId)
        {
            var prestamo = await GetPrestamosBaseQuery().FirstOrDefaultAsync(p => p.Id == prestamoId);
            if (prestamo == null)
            {
                throw new InvalidOperationException("Préstamo no encontrado.");
            }
            if (prestamo.Estado == EstadoPrestamo.Devuelto)
            {
                throw new InvalidOperationException("El préstamo ya ha sido devuelto.");
            }
            if (prestamo.Estado == EstadoPrestamo.Cancelado)
            {
                throw new InvalidOperationException("El préstamo ha sido cancelado y no puede ser devuelto.");
            }

            prestamo.FechaDevolucion = DateTime.UtcNow;
            prestamo.Estado = EstadoPrestamo.Devuelto;
            _context.Prestamos.Update(prestamo);

            if (prestamo.Libro != null)
            {
                prestamo.Libro.CopiasDisponibles++;
                _context.Libros.Update(prestamo.Libro);
            }

            await _context.SaveChangesAsync();

            // Desencadenar la lógica de reservas después de la devolución
            await ManejarReservasTrasDevolucion(prestamo.LibroId);

            return prestamo;
        }

        // Implementación para eliminar un préstamo
        public async Task<bool> EliminarPrestamoAsync(int id)
        {
            var prestamo = await _context.Prestamos.FindAsync(id);
            if (prestamo == null)
            {
                return false;
            }

            if (prestamo.Estado != EstadoPrestamo.Devuelto && prestamo.LibroId > 0)
            {
                var libro = await _context.Libros.FindAsync(prestamo.LibroId);
                if (libro != null)
                {
                    libro.CopiasDisponibles++;
                    _context.Libros.Update(libro);
                }
            }

            _context.Prestamos.Remove(prestamo);
            await _context.SaveChangesAsync();
            return true;
        }

        // Implementación para obtener libros disponibles
        public async Task<IEnumerable<Libro>> GetLibrosDisponiblesAsync()
        {
            return await _context.Libros.Where(l => l.CopiasDisponibles > 0).ToListAsync();
        }

        // Implementación para obtener todos los usuarios
        public async Task<IEnumerable<UsuarioAplicacion>> GetTodosUsuariosAsync()
        {
            return await _userManager.Users.ToListAsync();
        }

        // Implementación de lógica para verificar si el usuario puede pedir un libro prestado
        public Task<bool> PuedeUsuarioPedirPrestadoLibro(string usuarioId)
        {
            // Implementa tu lógica de negocio aquí.
            // Por ejemplo, verificar límites de préstamos activos, multas, etc.
            // Por ahora, siempre retorna true para permitir el préstamo.
            return Task.FromResult(true);
        }

        // Implementación para verificar si un libro tiene copias disponibles
        public async Task<bool> TieneLibroCopiasDisponibles(int libroId)
        {
            return await _context.Libros.AnyAsync(l => l.id == libroId && l.CopiasDisponibles > 0); // Asumo que la propiedad es 'Id'
        }

        // Implementación para obtener un libro por ID
        public async Task<Libro> GetLibroByIdAsync(int libroId)
        {
            return await _context.Libros.FindAsync(libroId);
        }

        // Implementación para obtener un usuario por ID
        public async Task<UsuarioAplicacion> GetUsuarioByIdAsync(string userId)
        {
            return await _userManager.FindByIdAsync(userId);
        }

        // **Nuevo método para manejar reservas después de una devolución**
        private async Task ManejarReservasTrasDevolucion(int libroId)
        {
            // Busca la reserva más antigua y activa para este libro
            var reservaPendiente = await _context.Reservas
                .Where(r => r.LibroId == libroId && r.Estado == EstadoReserva.Pendiente)
                .OrderBy(r => r.FechaReserva)
                .FirstOrDefaultAsync();

            if (reservaPendiente != null)
            {
                // Si hay una reserva pendiente, la marcamos como "Lista para Recoger"
                // Esto hará que el botón "Reclamar Libro" aparezca para el usuario correspondiente.
                reservaPendiente.Estado = EstadoReserva.ListaParaRecoger;
                _context.Reservas.Update(reservaPendiente);
                await _context.SaveChangesAsync();
            }
        }
    }
}
