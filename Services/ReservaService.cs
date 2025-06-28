using BBAPP.Data;
using BBAPP.Data.Models;
// using BBAPP.DTOs; // Ya no es necesario si no se usa ReservaDto
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity; // Necesario para UserManager si lo usas

namespace BBAPP.Services
{
    public class ReservaService : IReservaService
    {
        private readonly ProyectoBibliotecaContext _context;
        private readonly UserManager<UsuarioAplicacion> _userManager; // Si lo necesitas en este servicio

        public ReservaService(ProyectoBibliotecaContext context, UserManager<UsuarioAplicacion> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Obtiene el número total de reservas pendientes para un libro específico.
        public async Task<int> GetNumeroReservasParaLibroAsync(int libroId)
        {
            return await _context.Reservas.CountAsync(r => r.LibroId == libroId && r.Estado == EstadoReserva.Pendiente);
        }

        // Verifica si un usuario ya tiene una reserva activa (pendiente o lista para recoger) para un libro.
        public async Task<bool> UsuarioYaTieneReservaActiva(int libroId, string userId)
        {
            return await _context.Reservas.AnyAsync(r => r.LibroId == libroId && r.UsuarioId == userId &&
                                                         (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.ListaParaRecoger));
        }

        // Crea una nueva reserva. Ahora devuelve el modelo Reserva creado.
        public async Task<Reserva> CrearReservaAsync(int libroId, string userId)
        {
            // Validar que el libro exista
            var libro = await _context.Libros.FindAsync(libroId);
            if (libro == null)
            {
                throw new InvalidOperationException("Libro no encontrado.");
            }

            // Validar que el usuario exista (opcional, ya que userId viene de la autenticación)
            // var usuario = await _userManager.FindByIdAsync(userId);
            // if (usuario == null) { throw new InvalidOperationException("Usuario no encontrado."); }

            // Un usuario no debería poder reservar un libro si ya lo tiene prestado
            var prestamoActivo = await _context.Prestamos
                .AnyAsync(p => p.LibroId == libroId && p.UsuarioId == userId && p.Estado == EstadoPrestamo.Activo);
            if (prestamoActivo)
            {
                throw new InvalidOperationException("Ya tienes este libro prestado.");
            }

            // Un usuario no debería poder reservar si ya tiene una reserva activa para este libro
            if (await UsuarioYaTieneReservaActiva(libroId, userId))
            {
                throw new InvalidOperationException("Ya tienes una reserva activa para este libro.");
            }

            var nuevaReserva = new Reserva
            {
                LibroId = libroId,
                UsuarioId = userId,
                FechaReserva = DateTime.UtcNow,
                Estado = EstadoReserva.Pendiente, // Inicialmente, la reserva está pendiente
                FechaExpiracion = DateTime.UtcNow.AddDays(30) // Ejemplo de fecha de expiración
            };

            _context.Reservas.Add(nuevaReserva);
            await _context.SaveChangesAsync();
            return nuevaReserva; // Devuelve el modelo Reserva
        }

        // Obtiene la reserva activa más antigua para un libro y un usuario, si es el turno de ese usuario.
        // Ahora devuelve el modelo Reserva.
        public async Task<Reserva> GetReservaActivaParaLibroYUsuarioAsync(int libroId, string userId)
        {
            // Buscar la reserva del usuario que esté 'ListaParaRecoger'
            var reservaListaParaRecoger = await _context.Reservas
                .Include(r => r.Libro)
                .Include(r => r.Usuario)
                .Where(r => r.LibroId == libroId && r.UsuarioId == userId && r.Estado == EstadoReserva.ListaParaRecoger)
                .OrderBy(r => r.FechaReserva) // Asegura que sea la más antigua si hubiera varias (aunque idealmente solo debería haber una)
                .FirstOrDefaultAsync();

            if (reservaListaParaRecoger != null)
            {
                // Si encontramos una reserva 'ListaParaRecoger', verificamos si el libro tiene copias disponibles
                // y si esta reserva es la que realmente sigue en la cola de reservas pendientes.
                // Esto es para evitar que un usuario reclame si no le toca.
                var primeraReservaPendiente = await _context.Reservas
                    .Where(r => r.LibroId == libroId && r.Estado == EstadoReserva.Pendiente)
                    .OrderBy(r => r.FechaReserva)
                    .FirstOrDefaultAsync();

                if (reservaListaParaRecoger.Libro.CopiasDisponibles > 0 &&
                    (primeraReservaPendiente == null || primeraReservaPendiente.Id == reservaListaParaRecoger.Id))
                {
                    return reservaListaParaRecoger; // Devuelve el modelo Reserva
                }
            }
            return null; // No hay reserva que cumpla las condiciones para ser reclamada por este usuario.
        }

        // Procesa la reclamación de una reserva, convirtiéndola en un préstamo.
        // Devuelve el objeto Prestamo creado o null si falla.
        public async Task<Prestamo> ReclamarReservaAsync(int reservaId)
        {
            var reserva = await _context.Reservas
                                        .Include(r => r.Libro)
                                        .Include(r => r.Usuario)
                                        .FirstOrDefaultAsync(r => r.Id == reservaId);

            if (reserva == null)
            {
                throw new InvalidOperationException("Reserva no encontrada.");
            }

            if (reserva.Estado != EstadoReserva.ListaParaRecoger)
            {
                throw new InvalidOperationException("La reserva no está lista para ser reclamada.");
            }

            if (reserva.Libro == null || reserva.Libro.CopiasDisponibles <= 0)
            {
                throw new InvalidOperationException("No hay copias disponibles del libro para reclamar la reserva.");
            }

            // Validar que el usuario no tenga ya un préstamo activo de este libro
            var prestamoActivo = await _context.Prestamos
                .AnyAsync(p => p.LibroId == reserva.LibroId && p.UsuarioId == reserva.UsuarioId && p.Estado == EstadoPrestamo.Activo);
            if (prestamoActivo)
            {
                throw new InvalidOperationException("Ya tienes este libro prestado. No puedes reclamar la reserva.");
            }

            // Crear un nuevo préstamo
            var nuevoPrestamo = new Prestamo
            {
                LibroId = reserva.LibroId,
                UsuarioId = reserva.UsuarioId,
                FechaPrestamo = DateTime.UtcNow,
                FechaVencimiento = DateTime.UtcNow.AddDays(14), // Por ejemplo, 14 días de préstamo
                Estado = EstadoPrestamo.Activo
            };

            _context.Prestamos.Add(nuevoPrestamo);

            // Actualizar el estado de la reserva a "Cumplida"
            reserva.Estado = EstadoReserva.Cumplida; // Usar EstadoReserva.Cumplida para coincidir con el enum
            reserva.FechaExpiracion = DateTime.UtcNow; // La reserva se cumple hoy
            // reserva.PrestamoId = nuevoPrestamo.Id; // Si tienes una FK de Reserva a Prestamo
            _context.Reservas.Update(reserva);

            // Disminuir la copia disponible del libro
            reserva.Libro.CopiasDisponibles--;
            _context.Libros.Update(reserva.Libro);

            await _context.SaveChangesAsync();
            return nuevoPrestamo; // Devuelve el préstamo creado
        }

        // Cancela una reserva.
        public async Task<bool> CancelarReservaAsync(int reservaId)
        {
            var reserva = await _context.Reservas.FindAsync(reservaId);
            if (reserva == null) return false;

            // Solo se pueden cancelar reservas pendientes o listas para recoger
            if (reserva.Estado == EstadoReserva.Pendiente || reserva.Estado == EstadoReserva.ListaParaRecoger)
            {
                reserva.Estado = EstadoReserva.Cancelada;
                reserva.FechaExpiracion = DateTime.UtcNow; // Marcar como cancelada hoy
                _context.Reservas.Update(reserva);
                await _context.SaveChangesAsync();
                return true;
            }
            return false; // No se puede cancelar una reserva que ya está cumplida o cancelada
        }

        // Obtiene todas las reservas como modelos.
        public async Task<IEnumerable<Reserva>> GetTodasReservasAsync()
        {
            return await _context.Reservas
                                .Include(r => r.Libro)
                                .Include(r => r.Usuario)
                                .OrderByDescending(r => r.FechaReserva)
                                .ToListAsync();
        }

        // Obtiene las reservas de un usuario específico como modelos.
        public async Task<IEnumerable<Reserva>> GetReservasPorUsuarioAsync(string userId)
        {
            return await _context.Reservas
                                 .Include(r => r.Libro)
                                 .Include(r => r.Usuario)
                                 .Where(r => r.UsuarioId == userId)
                                 .OrderByDescending(r => r.FechaReserva)
                                 .ToListAsync();
        }

        // Obtiene una reserva por su ID como modelo.
        public async Task<Reserva> GetReservaByIdAsync(int reservaId)
        {
            var reserva = await _context.Reservas
                                        .Include(r => r.Libro)
                                        .Include(r => r.Usuario)
                                        .FirstOrDefaultAsync(r => r.Id == reservaId);
            return reserva;
        }
    }
}

