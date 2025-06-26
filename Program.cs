using BBAPP.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;  // <<– Asegúrate de importar esto

var builder = WebApplication.CreateBuilder(args);

// 1. Registrar el DbContext con la cadena de conexión
builder.Services.AddDbContext<ProyectoBibliotecaContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("BibliotecaConnection")
    ));

// 2. Servicios de Blazor
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// (Opcional) remueve o adapta este servicio si no lo necesitas
builder.Services.AddSingleton<WeatherForecastService>();

var app = builder.Build();

// 3. Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// 4. Endpoints de Blazor
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
