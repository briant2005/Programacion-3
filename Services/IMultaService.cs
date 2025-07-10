using BBAPP.Data.Models;

namespace BBAPP.Services
{
    public interface IMultaService
    {
        Task<IEnumerable<Multa>> GetMultasPorUsuarioAsync(string userId);
        Task<IEnumerable<Multa>> GetMultasPendientesPorUsuarioAsync(string userId);
        Task<Multa> GetMultaByIdAsync(int multaId);
        Task<bool> MarcarMultaComoPagadaAsync(int multaId);
    }
}
