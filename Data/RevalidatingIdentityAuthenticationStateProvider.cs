using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
// No necesitamos directamente BBAPP.Data.Models aquí porque TUser lo reemplazará.
// Solo lo necesitamos si UsuarioAplicacion es el único tipo de usuario.

namespace BBAPP.Data
{
    // Modificado: Ahora la clase es genérica con TUser.
    // TUser debe ser un tipo que herede de IdentityUser, como tu UsuarioAplicacion.
    public class RevalidatingIdentityAuthenticationStateProvider<TUser> : RevalidatingServerAuthenticationStateProvider
        where TUser : IdentityUser
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RevalidatingIdentityAuthenticationStateProvider<TUser>> _logger; // Logger también es genérico

        public RevalidatingIdentityAuthenticationStateProvider(
            ILoggerFactory loggerFactory,
            IServiceScopeFactory scopeFactory)
            : base(loggerFactory)
        {
            _scopeFactory = scopeFactory;
            _logger = loggerFactory.CreateLogger<RevalidatingIdentityAuthenticationStateProvider<TUser>>();
        }

        protected override TimeSpan RevalidationInterval => TimeSpan.FromSeconds(30);

        protected override async Task<bool> ValidateAuthenticationStateAsync(
            AuthenticationState authenticationState, CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            // Modificado: El UserManager también es genérico con TUser
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();

            return await ValidateSecurityStampAsync(userManager, authenticationState.User);
        }

        private async Task<bool> ValidateSecurityStampAsync(UserManager<TUser> userManager, ClaimsPrincipal principal)
        {
            var user = await userManager.GetUserAsync(principal);

            if (user == null)
            {
                _logger.LogWarning("Validation failed: User not found.");
                return false;
            }
            else if (!userManager.SupportsUserSecurityStamp)
            {
                _logger.LogInformation("Validation skipped: Security stamp not supported.");
                return true;
            }
            else
            {
                var principalStamp = principal.FindFirstValue(userManager.Options.ClaimsIdentity.SecurityStampClaimType);
                var userStamp = await userManager.GetSecurityStampAsync(user);

                bool isValid = string.Equals(principalStamp, userStamp);

                if (!isValid)
                {
                    _logger.LogWarning("Validation failed: Security stamp mismatch.");
                }
                return isValid;
            }
        }
    }
}
