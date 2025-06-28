using BBAPP.Data.Models;
using BBAPP.DTOs;
using Microsoft.AspNetCore.Identity;

namespace BBAPP.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<UsuarioAplicacion> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AuthService(UserManager<UsuarioAplicacion> userManager,
                           RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IdentityResult> RegistrarUsuarioAsync(RegistroUsuarioDto modelo)
        {
            var usuario = new UsuarioAplicacion
            {
                UserName = modelo.NombreUsuario,
                Email = modelo.Email,
                Nombre = modelo.Nombre,
                Apellido = modelo.Apellido
            };

            var resultado = await _userManager.CreateAsync(usuario, modelo.Contrasena);

            if (resultado.Succeeded)
            {
                await _userManager.AddToRoleAsync(usuario, "Usuario");
            }
            return resultado;
        }

        public async Task<IdentityResult> AsignarRolAUsuarioAsync(string usuarioId, string nombreRol)
        {
            var usuario = await _userManager.FindByIdAsync(usuarioId);
            if (usuario == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Usuario no encontrado." });
            }

            if (!await _roleManager.RoleExistsAsync(nombreRol))
            {
                return IdentityResult.Failed(new IdentityError { Description = $"El rol '{nombreRol}' no existe." });
            }

            var resultado = await _userManager.AddToRoleAsync(usuario, nombreRol);
            return resultado;
        }

        public async Task<IdentityResult> RemoverRolDeUsuarioAsync(string usuarioId, string nombreRol)
        {
            var usuario = await _userManager.FindByIdAsync(usuarioId);
            if (usuario == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Usuario no encontrado." });
            }

            if (!await _roleManager.RoleExistsAsync(nombreRol))
            {
                return IdentityResult.Failed(new IdentityError { Description = $"El rol '{nombreRol}' no existe." });
            }

            var resultado = await _userManager.RemoveFromRoleAsync(usuario, nombreRol);
            return resultado;
        }

        // --- MÉTODO CORREGIDO ---
        public Task<IEnumerable<RolDto>> ObtenerTodosLosRolesAsync() // <-- Ahora devuelve un Task
        {
            // La operación de obtención de roles es síncrona aquí, pero la interfaz espera un Task.
            // Envolvemos el resultado en Task.FromResult() para satisfacer la interfaz.
            var roles = _roleManager.Roles.Select(r => new RolDto { Id = r.Id, Nombre = r.Name }).ToList();
            return Task.FromResult(roles as IEnumerable<RolDto>);
        }

        public async Task<IEnumerable<string>> ObtenerRolesDeUsuarioAsync(string usuarioId)
        {
            var usuario = await _userManager.FindByIdAsync(usuarioId);
            if (usuario == null)
            {
                return Enumerable.Empty<string>();
            }
            return await _userManager.GetRolesAsync(usuario);
        }
    }
}
