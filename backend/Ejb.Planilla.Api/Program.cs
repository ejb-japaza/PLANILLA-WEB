using System.Text.Json.Serialization;
using Ejb.Planilla.Api.Compartido.Datos;
using Ejb.Planilla.Api.Compartido.Salud;
using Ejb.Planilla.Api.Compartido.Seguridad;
using Ejb.Planilla.Api.Modulos.Acceso;
using Ejb.Planilla.Api.Modulos.Acceso.CrearContrasenaWeb;
using Ejb.Planilla.Api.Modulos.Acceso.IniciarSesion;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Logs: configuración en la sección "Serilog" de appsettings
builder.Host.UseSerilog((contexto, logs) => logs.ReadFrom.Configuration(contexto.Configuration));

// Servicios comunes
builder.Services.AddControllers()
    // Los campos vacíos (null) no se envían en las respuestas JSON
    .AddJsonOptions(o => o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<ICn, Cn>();
builder.Services.AddSeguridad(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddCheck<SaludBaseDatos>("base-datos")
    .AddCheck<SaludCambiosBd>("cambios-bd");

builder.Services.AddValidatorsFromAssemblyContaining<Program>(); // registra todos los validadores

// Módulos: cada uno registra aquí sus servicios

// Acceso
builder.Services.AddSingleton<HashContrasena>();
builder.Services.AddSingleton<EmisorTokens>();
builder.Services.AddScoped<IUsuarioRepo, UsuarioRepo>();
builder.Services.AddScoped<IniciarSesionServicio>();
builder.Services.AddScoped<CrearContrasenaWebServicio>();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    // Especificación OpenAPI para importar en Postman (solo en desarrollo)
    app.MapOpenApi().AllowAnonymous();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/salud", new HealthCheckOptions { ResponseWriter = RespuestaSalud.Escribir })
    .AllowAnonymous();

app.Run();

/// <summary>Punto de entrada; público para las pruebas.</summary>
public partial class Program;
