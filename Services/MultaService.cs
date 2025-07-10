using BBAPP.Data.Models;
using BBAPP.Data;
using Microsoft.EntityFrameworkCore;

namespace BBAPP.Services
{
    public class MultaService : IMultaService
    {
        private readonly ProyectoBibliotecaContext _context;

        public MultaService(ProyectoBibliotecaContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Multa>> GetMultasPorUsuarioAsync(string userId)
        {
            return await _context.Multas
                                 .Include(m => m.Prestamo) // Incluir el préstamo para acceder al libro
                                     .ThenInclude(p => p.Libro) // Incluir el libro del préstamo
                                 .Where(m => m.UsuarioId == userId)
                                 .OrderByDescending(m => m.FechaMulta)
                                 .ToListAsync();
        }

        public async Task<IEnumerable<Multa>> GetMultasPendientesPorUsuarioAsync(string userId)
        {
            return await _context.Multas
                                 .Include(m => m.Prestamo)
                                     .ThenInclude(p => p.Libro)
                                 .Where(m => m.UsuarioId == userId && !m.Pagada)
                                 .OrderByDescending(m => m.FechaMulta)
                                 .ToListAsync();
        }

        public async Task<Multa> GetMultaByIdAsync(int multaId)
        {
            return await _context.Multas
                                 .Include(m => m.Prestamo)
                                     .ThenInclude(p => p.Libro)
                                 .FirstOrDefaultAsync(m => m.Id == multaId);
        }

        public async Task<bool> MarcarMultaComoPagadaAsync(int multaId)
        {
            var multa = await _context.Multas.FindAsync(multaId);
            if (multa == null || multa.Pagada)
            {
                return false; // Multa no encontrada o ya pagada
            }

            multa.Pagada = true;
            _context.Multas.Update(multa);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
