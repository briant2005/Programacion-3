using BBAPP;
using BBAPP.Data;
using BBAPP.Data.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;  // <<– Asegúrate de importar esto
using Microsoft.AspNetCore.Components.Authorization; // Necesario para el componente CascadingAuthenticationState en App.razor
using BBAPP.Services; // Namespace para tus servicios de lógica de negocio
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// 1. Registrar el DbContext con la cadena de conexión
builder.Services.AddDbContext<ProyectoBibliotecaContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("BibliotecaConnection")
    ));


// 2.Configurar ASP.NET Core Identity
builder.Services.AddDefaultIdentity<UsuarioAplicacion>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
options.Password.RequireDigit = true;
options.Password.RequireLowercase = true;
options.Password.RequireUppercase = true;
options.Password.RequiredLength = 8;
options.User.RequireUniqueEmail = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ProyectoBibliotecaContext>();

// 3. Servicios de Blazor Server
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Habilitar la autorización en componentes Blazor
builder.Services.AddAuthorization();
// ELIMINAMOS ESTA LÍNEA QUE DABA PROBLEMAS:
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<UsuarioAplicacion>>();

// 4. Registrar tus servicios de lógica de negocio (Interfaces e Implementaciones)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ILibroService, LibroService>();
builder.Services.AddScoped<IPrestamoService, PrestamoService>(); // CORREGIDO: IPrestamoService implementado por PrestamoService
builder.Services.AddScoped<IReservaService, ReservaService>(); // <--- ¡ASEGÚRATE DE QUE ESTA LÍNEA ESTÉ PRESENTE Y DESCOMENTADA!


var app = builder.Build();

// 5. Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
    var body = await reader.ReadToEndAsync();
    context.Request.Body.Position = 0;
    Console.WriteLine($"Raw POST Body: {body}");
    await next();
});

app.UseRouting();

// IMPORTANTE: Middleware de autenticación y autorización para una app Blazor Server
app.UseAuthentication();
app.UseAuthorization();

// 6. Endpoints de Blazor
app.MapControllers();
app.MapRazorPages();          // ← Must be added
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Aplicar migraciones y sembrar roles al inicio (para desarrollo)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ProyectoBibliotecaContext>();
        context.Database.Migrate();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<UsuarioAplicacion>>();

        string[] roleNames = { "Admin", "Bibliotecario", "Usuario" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        var adminUserEmail = "admin@example.com";
        var adminUser = await userManager.FindByEmailAsync(adminUserEmail);
        if (adminUser == null)
        {
            adminUser = new UsuarioAplicacion
            {
                UserName = adminUserEmail,
                Email = adminUserEmail,
                Nombre = "Super",
                Apellido = "Admin",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(adminUser, "AdminP@ssword123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al sembrar la base de datos o aplicar migraciones.");
    }
}

app.Run();
