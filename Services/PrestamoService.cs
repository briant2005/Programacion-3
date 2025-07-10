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

        // Constantes para las reglas de préstamo
        private const int MAX_LIBROS_POR_USUARIO = 3; // Límite de libros que un usuario puede tener prestados
        private const int DIAS_MAX_PRESTAMO = 14; // Tiempo máximo de préstamo en días
        private const decimal TARIFA_MULTA_DIARIA = 0.50m; // Tarifa de multa por día de retraso

        // Implementación de la nueva propiedad
        public int MaxLibrosPorUsuario => MAX_LIBROS_POR_USUARIO;

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

        // Nuevo: Obtiene solo los préstamos pendientes de aprobación
        public async Task<IEnumerable<PrestamoDto>> GetPrestamosPendientesAprobacionAsync()
        {
            return (await GetPrestamosBaseQuery()
                               .Where(p => p.Estado == EstadoPrestamo.PendienteAprobacion)
                               .OrderBy(p => p.FechaPrestamo) // Ordenar por fecha de solicitud
                               .ToListAsync())
                       .Select(p => MapToPrestamoDto(p));
        }

        // Implementación para realizar un préstamo (ahora establece el estado como PendienteAprobacion)
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

            // Validar reglas de préstamo antes de permitir la solicitud
            if (!await PuedeUsuarioPedirPrestadoLibro(nuevoPrestamo.UsuarioId))
            {
                throw new InvalidOperationException("El usuario ha alcanzado su límite de préstamos o tiene libros vencidos.");
            }

            // Disminuir la copia disponible del libro inmediatamente para "reservarla"
            // Esto evita que otro usuario solicite el mismo libro mientras está pendiente de aprobación.
            libro.CopiasDisponibles--;
            _context.Libros.Update(libro);

            nuevoPrestamo.FechaPrestamo = DateTime.UtcNow;
            nuevoPrestamo.FechaVencimiento = DateTime.UtcNow.AddDays(DIAS_MAX_PRESTAMO); // Usar la constante
            nuevoPrestamo.Estado = EstadoPrestamo.PendienteAprobacion; // CAMBIO CLAVE: Estado inicial
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
            if (prestamo.Estado == EstadoPrestamo.PendienteAprobacion) // No se puede devolver un préstamo pendiente
            {
                throw new InvalidOperationException("El préstamo está pendiente de aprobación y no puede ser devuelto.");
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

            // Calcular y registrar multa si aplica
            if (prestamo.FechaDevolucion > prestamo.FechaVencimiento)
            {
                await CalcularYRegistrarMultaAsync(prestamoId);
            }

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

            // Si el préstamo está activo o pendiente de aprobación, devolver la copia al catálogo
            if (prestamo.Estado == EstadoPrestamo.Activo || prestamo.Estado == EstadoPrestamo.PendienteAprobacion)
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

        // Nuevo: Método para aprobar una solicitud de préstamo
        public async Task<Prestamo> AprobarPrestamoAsync(int prestamoId)
        {
            var prestamo = await _context.Prestamos
                                         .Include(p => p.Libro) // Incluir el libro para posibles actualizaciones
                                         .FirstOrDefaultAsync(p => p.Id == prestamoId);

            if (prestamo == null)
            {
                throw new InvalidOperationException("Solicitud de préstamo no encontrada.");
            }

            if (prestamo.Estado != EstadoPrestamo.PendienteAprobacion)
            {
                throw new InvalidOperationException("El préstamo no está en estado 'Pendiente de Aprobación'.");
            }

            // Opcional: Re-validar las reglas aquí si es necesario (ej. si el usuario ya tiene libros vencidos al momento de la aprobación)
            // if (!await PuedeUsuarioPedirPrestadoLibro(prestamo.UsuarioId))
            // {
            //     throw new InvalidOperationException("El usuario ya no cumple los requisitos para este préstamo.");
            // }

            prestamo.Estado = EstadoPrestamo.Activo; // Cambiar el estado a Activo
            _context.Prestamos.Update(prestamo);
            await _context.SaveChangesAsync();

            return prestamo;
        }

        // Nuevo: Método para denegar una solicitud de préstamo
        public async Task<Prestamo> DenegarPrestamoAsync(int prestamoId)
        {
            var prestamo = await _context.Prestamos
                                         .Include(p => p.Libro) // Incluir el libro para devolver la copia
                                         .FirstOrDefaultAsync(p => p.Id == prestamoId);

            if (prestamo == null)
            {
                throw new InvalidOperationException("Solicitud de préstamo no encontrada.");
            }

            if (prestamo.Estado != EstadoPrestamo.PendienteAprobacion)
            {
                throw new InvalidOperationException("El préstamo no está en estado 'Pendiente de Aprobación'.");
            }

            prestamo.Estado = EstadoPrestamo.Denegado; // Cambiar el estado a Denegado
            _context.Prestamos.Update(prestamo);

            // Devolver la copia al catálogo si se había disminuido al solicitar
            if (prestamo.Libro != null)
            {
                prestamo.Libro.CopiasDisponibles++;
                _context.Libros.Update(prestamo.Libro);
            }

            await _context.SaveChangesAsync();
            return prestamo;
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
        public async Task<bool> PuedeUsuarioPedirPrestadoLibro(string usuarioId)
        {
            // Obtener todos los préstamos activos y pendientes de aprobación del usuario
            var prestamosActivosOPendientes = await _context.Prestamos
                                                 .Where(p => p.UsuarioId == usuarioId &&
                                                            (p.Estado == EstadoPrestamo.Activo || p.Estado == EstadoPrestamo.PendienteAprobacion))
                                                 .ToListAsync();

            // 1. Límite de libros por usuario (contando activos y pendientes de aprobación)
            if (prestamosActivosOPendientes.Count >= MAX_LIBROS_POR_USUARIO)
            {
                return false; // Ha alcanzado el límite de préstamos (incluyendo los pendientes)
            }

            // 2. Restricción de nuevos préstamos si hay libros vencidos
            var tieneLibrosVencidos = prestamosActivosOPendientes.Any(p => p.FechaVencimiento < DateTime.UtcNow && p.Estado == EstadoPrestamo.Activo);
            if (tieneLibrosVencidos)
            {
                return false; // Tiene al menos un libro vencido (solo se considera activos para vencimiento)
            }

            return true; // El usuario cumple con las reglas para pedir prestado
        }

        // Nuevo: Verifica si el usuario ha alcanzado el límite de préstamos (incluyendo pendientes)
        public async Task<bool> HasReachedLoanLimitAsync(string userId)
        {
            var prestamosActivosOPendientes = await _context.Prestamos
                                                 .Where(p => p.UsuarioId == userId &&
                                                            (p.Estado == EstadoPrestamo.Activo || p.Estado == EstadoPrestamo.PendienteAprobacion))
                                                 .ToListAsync();
            return prestamosActivosOPendientes.Count >= MAX_LIBROS_POR_USUARIO;
        }

        // Nuevo: Verifica si el usuario tiene préstamos vencidos (solo activos)
        public async Task<bool> HasOverdueLoansAsync(string userId)
        {
            return await _context.Prestamos
                                 .AnyAsync(p => p.UsuarioId == userId && p.Estado == EstadoPrestamo.Activo && p.FechaVencimiento < DateTime.UtcNow);
        }

        // Implementación para verificar si un libro tiene copias disponibles
        public async Task<bool> TieneLibroCopiasDisponibles(int libroId)
        {
            return await _context.Libros.AnyAsync(l => l.id == libroId && l.CopiasDisponibles > 0);
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

        // Nuevo método para calcular y registrar multas
        public async Task<Multa> CalcularYRegistrarMultaAsync(int prestamoId)
        {
            var prestamo = await _context.Prestamos
                                         .Include(p => p.Usuario)
                                         .FirstOrDefaultAsync(p => p.Id == prestamoId);

            if (prestamo == null || prestamo.FechaDevolucion == null || prestamo.FechaDevolucion <= prestamo.FechaVencimiento)
            {
                return null; // No hay multa o el préstamo no existe/no ha sido devuelto a tiempo
            }

            // Calcular días de retraso
            TimeSpan diasAtraso = prestamo.FechaDevolucion.Value - prestamo.FechaVencimiento;
            int diasRetrasoEnteros = (int)Math.Ceiling(diasAtraso.TotalDays);

            if (diasRetrasoEnteros <= 0)
            {
                return null; // No hay retraso real
            }

            decimal montoMulta = diasRetrasoEnteros * TARIFA_MULTA_DIARIA;

            var nuevaMulta = new Multa
            {
                PrestamoId = prestamo.Id,
                UsuarioId = prestamo.UsuarioId,
                FechaMulta = DateTime.UtcNow,
                Monto = montoMulta,
                Pagada = false // Inicialmente la multa no está pagada
            };

            _context.Multas.Add(nuevaMulta);
            await _context.SaveChangesAsync();

            return nuevaMulta;
        }

        // Método para manejar reservas después de una devolución
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
