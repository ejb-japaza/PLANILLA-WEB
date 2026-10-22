using Ejb.Planilla.Api.Compartido;
using Ejb.Planilla.Api.Compartido.Seguridad;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ejb.Planilla.Api.Modulos.Acceso.ElegirContexto;

/// <summary>
/// Fase 2 del login. Con el token de acceso de la fase 1:
///   GET  /api/acceso/contexto/opciones → qué puede elegir
///   POST /api/acceso/contexto          → elige y recibe un token CON contexto (también sirve para cambiar de compañía)
/// Con el token con contexto:
///   GET  /api/acceso/contexto          → contexto actual (exige ConContexto)
/// </summary>
[ApiController]
[Route("api/acceso/contexto")]
public sealed class ElegirContextoController : ControllerBase
{
    private readonly IValidator<ElegirContextoSolicitud> _validador;
    private readonly ElegirContextoServicio _servicio;

    public ElegirContextoController(IValidator<ElegirContextoSolicitud> validador, ElegirContextoServicio servicio)
    {
        _validador = validador;
        _servicio = servicio;
    }

    [HttpGet("opciones")]
    public async Task<IActionResult> Opciones(CancellationToken ct)
    {
        var resultado = await _servicio.Opciones(User.WsCodUsu(), ct);
        return resultado.Exito ? Ok(resultado.Valor) : Error(resultado.Error!);
    }

    [HttpPost]
    public async Task<IActionResult> Elegir(ElegirContextoSolicitud solicitud, CancellationToken ct)
    {
        var validacion = await _validador.ValidateAsync(solicitud, ct);
        if (!validacion.IsValid)
            return ValidationProblem(new ValidationProblemDetails(validacion.ToDictionary()));

        var resultado = await _servicio.Elegir(User.WsCodUsu(), solicitud, ct);
        return resultado.Exito ? Ok(resultado.Valor) : Error(resultado.Error!);
    }

    /// <summary>Contexto del token actual. Ejemplo de endpoint protegido con ConContexto (así serán los módulos).</summary>
    [HttpGet]
    [Authorize(Policy = ConfiguracionSeguridad.ConContexto)]
    public IActionResult Actual() => Ok(new
    {
        codUsu = User.WsCodUsu(),
        codCom = User.WsCodCom(),
        codSuc = User.WsCodSuc(),
        tipTra = User.WsTipTra(),
        perAct = User.WsPerAct(),
    });

    /// <summary>401: sesión no válida · 403: sin acceso a la compañía/sucursal · 422: faltan datos de configuración.</summary>
    private ObjectResult Error(Falla falla)
    {
        var estado = falla.Codigo switch
        {
            "Acceso.UsuarioNoValido" => StatusCodes.Status401Unauthorized,
            "Acceso.SinSucursal" or "Acceso.SinPeriodo" => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status403Forbidden,
        };
        return Problem(title: falla.Mensaje, statusCode: estado);
    }
}
