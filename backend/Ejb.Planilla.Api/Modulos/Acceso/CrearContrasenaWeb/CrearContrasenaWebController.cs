using Ejb.Planilla.Api.Compartido.Seguridad;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Ejb.Planilla.Api.Modulos.Acceso.CrearContrasenaWeb;

/// <summary>
/// POST /api/acceso/contrasena. Solo acepta el token de cambio de contraseña (tokenContrasena del login),
/// enviado como "Authorization: Bearer ...". El token de acceso NO sirve aquí.
/// </summary>
[ApiController]
[Route("api/acceso/contrasena")]
[Authorize(AuthenticationSchemes = ConfiguracionSeguridad.EsquemaContrasena)]
[EnableRateLimiting(ConfiguracionSeguridad.LimiteLogin)]
public sealed class CrearContrasenaWebController : ControllerBase
{
    private readonly IValidator<CrearContrasenaWebSolicitud> _validador;
    private readonly CrearContrasenaWebServicio _servicio;

    public CrearContrasenaWebController(IValidator<CrearContrasenaWebSolicitud> validador, CrearContrasenaWebServicio servicio)
    {
        _validador = validador;
        _servicio = servicio;
    }

    [HttpPost]
    public async Task<IActionResult> Post(CrearContrasenaWebSolicitud solicitud, CancellationToken ct)
    {
        // 400: vacía, demasiado larga o confirmación distinta
        var validacion = await _validador.ValidateAsync(solicitud, ct);
        if (!validacion.IsValid)
            return ValidationProblem(new ValidationProblemDetails(validacion.ToDictionary()));

        // El usuario sale del token ya validado, nunca del cuerpo
        var wsCodUsu = User.FindFirst(ClaimsPlanilla.Usuario)!.Value;

        var resultado = await _servicio.Ejecutar(wsCodUsu, solicitud, ct);
        if (!resultado.Exito)
            return Problem(title: resultado.Error!.Mensaje, statusCode: StatusCodes.Status401Unauthorized);

        var respuesta = resultado.Valor!;
        CookieRenovacion.Escribir(Response, respuesta.TokenRenovacion!);
        return Ok(respuesta);
    }
}
