using BBAPP.DTOs;
using Microsoft.AspNetCore.Identity;

namespace BBAPP.Services
{
    public interface IAuthService
    {
        Task<IdentityResult> RegistrarUsuarioAsync(RegistroUsuarioDto modelo);
        Task<IdentityResult> AsignarRolAUsuarioAsync(string usuarioId, string nombreRol);
        Task<IdentityResult> RemoverRolDeUsuarioAsync(string usuarioId, string nombreRol);
        Task<IEnumerable<RolDto>> ObtenerTodosLosRolesAsync();
        Task<IEnumerable<string>> ObtenerRolesDeUsuarioAsync(string usuarioId);
    }
}
