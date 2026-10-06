using Ejb.Planilla.Api.Compartido;
using Ejb.Planilla.Api.Modulos.Acceso.IniciarSesion;

namespace Ejb.Planilla.Api.Modulos.Acceso.CrearContrasenaWeb;

/// <summary>Primer ingreso: guarda la contraseña web (hash en US1_CPASWBH) y deja al usuario dentro.</summary>
public sealed class CrearContrasenaWebServicio
{
    private readonly IUsuarioRepo _repo;
    private readonly HashContrasena _hash;
    private readonly EmisorTokens _emisor;
    private readonly ILogger<CrearContrasenaWebServicio> _log;

    public CrearContrasenaWebServicio(IUsuarioRepo repo, HashContrasena hash, EmisorTokens emisor, ILogger<CrearContrasenaWebServicio> log)
    {
        _repo = repo;
        _hash = hash;
        _emisor = emisor;
        _log = log;
    }

    /// <param name="wsCodUsu">Código de usuario tomado del token de cambio de contraseña (no del cuerpo).</param>
    public async Task<Resultado<IniciarSesionRespuesta>> Ejecutar(string wsCodUsu, CrearContrasenaWebSolicitud solicitud, CancellationToken ct)
    {
        // 1. El usuario debe seguir existiendo, activo y SIN contraseña web
        //    (evita reutilizar el token de 5 min para cambiar una contraseña ya creada)
        var usuario = await _repo.Buscar(wsCodUsu, ct);
        if (usuario is null || !usuario.EstaActivo || !usuario.DebeCrearContrasena)
        {
            _log.LogWarning("Creación de contraseña web rechazada para {wsCodUsu}", wsCodUsu);
            return Resultado<IniciarSesionRespuesta>.Fallo(ErroresAcceso.ContrasenaNoPermitida);
        }

        // 2. Guardar el hash (la contraseña nunca se guarda en texto)
        var hashWeb = _hash.Calcular(usuario.CodUsu, solicitud.Nueva);
        await _repo.GuardarHashWeb(usuario.CodUsu, hashWeb, ct);
        _log.LogInformation("Contraseña web creada para {wsCodUsu}", usuario.CodUsu);

        // 3. Queda dentro: mismos tokens que un login correcto
        var acceso = _emisor.Acceso(usuario.CodUsu, usuario.NomUsu);
        var renovacion = _emisor.Renovacion(usuario.CodUsu, EmisorTokens.Sello(hashWeb, usuario.EstadoUsu));

        return Resultado<IniciarSesionRespuesta>.Ok(new IniciarSesionRespuesta
        {
            TokenAcceso = acceso.Token,
            ExpiraEn = (int)Math.Round((acceso.Expira - DateTimeOffset.UtcNow).TotalSeconds),
            Usuario = new UsuarioSesion(usuario.CodUsu, usuario.NomUsu, usuario.EsMaestro),
            TokenRenovacion = renovacion,
        });
    }
}
