using Ejb.Planilla.Api.Compartido.Seguridad;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Ejb.Planilla.Api.Modulos.Acceso.IniciarSesion;

/// <summary>POST /api/acceso/login. Delgado: valida, llama al servicio y traduce el resultado a HTTP.</summary>
[ApiController]
[Route("api/acceso/login")]
[AllowAnonymous]
[EnableRateLimiting(ConfiguracionSeguridad.LimiteLogin)]
public sealed class IniciarSesionController : ControllerBase
{
    private readonly IValidator<IniciarSesionSolicitud> _validador;
    private readonly IniciarSesionServicio _servicio;

    public IniciarSesionController(IValidator<IniciarSesionSolicitud> validador, IniciarSesionServicio servicio)
    {
        _validador = validador;
        _servicio = servicio;
    }

    [HttpPost]
    public async Task<IActionResult> Post(IniciarSesionSolicitud solicitud, CancellationToken ct)
    {
        // 400: datos vacíos o demasiado largos
        var validacion = await _validador.ValidateAsync(solicitud, ct);
        if (!validacion.IsValid)
            return ValidationProblem(new ValidationProblemDetails(validacion.ToDictionary()));

        var resultado = await _servicio.Ejecutar(solicitud, ct);

        // 401: siempre el mismo mensaje, sin revelar el motivo
        if (!resultado.Exito)
            return Problem(title: resultado.Error!.Mensaje, statusCode: StatusCodes.Status401Unauthorized);

        var respuesta = resultado.Valor!;
        if (respuesta.TokenRenovacion is not null)
            CookieRenovacion.Escribir(Response, respuesta.TokenRenovacion);

        return Ok(respuesta);
    }
}
