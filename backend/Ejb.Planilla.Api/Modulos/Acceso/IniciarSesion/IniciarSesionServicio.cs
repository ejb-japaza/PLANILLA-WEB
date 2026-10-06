using System.Security.Cryptography;
using System.Text;
using Ejb.Planilla.Api.Compartido;
using Microsoft.AspNetCore.Identity;

namespace Ejb.Planilla.Api.Modulos.Acceso.IniciarSesion;

/// <summary>Lógica del login (equivale a la validación de GENPAS01 del VB6, sin contraseña maestra ni SQL concatenado).</summary>
public sealed class IniciarSesionServicio
{
    /// <summary>Hash de relleno: se verifica cuando el usuario no existe, para que la respuesta tarde lo mismo.</summary>
    private static readonly string HashRelleno = new HashContrasena().Calcular("-", Guid.NewGuid().ToString());

    private readonly IUsuarioRepo _repo;
    private readonly HashContrasena _hash;
    private readonly EmisorTokens _emisor;
    private readonly ILogger<IniciarSesionServicio> _log;

    public IniciarSesionServicio(IUsuarioRepo repo, HashContrasena hash, EmisorTokens emisor, ILogger<IniciarSesionServicio> log)
    {
        _repo = repo;
        _hash = hash;
        _emisor = emisor;
        _log = log;
    }

    public async Task<Resultado<IniciarSesionRespuesta>> Ejecutar(IniciarSesionSolicitud solicitud, CancellationToken ct)
    {
        var wsCodUsu = solicitud.Usuario.Trim();
        var wsPasUsu = solicitud.Contrasena;

        // 1. Buscar el usuario
        var usuario = await _repo.Buscar(wsCodUsu, ct);
        if (usuario is null)
        {
            _hash.Verificar(wsCodUsu, HashRelleno, wsPasUsu);
            _log.LogInformation("Login rechazado: usuario {wsCodUsu} no existe", wsCodUsu);
            return Resultado<IniciarSesionRespuesta>.Fallo(ErroresAcceso.CredencialesInvalidas);
        }

        // 2. Primer ingreso: aún no tiene contraseña web → validar una vez contra la del VB6
        if (usuario.DebeCrearContrasena)
        {
            if (!IgualContrasenaVb6(usuario.PasUsu, wsPasUsu))
                return Rechazar(usuario, "contraseña VB6 incorrecta (primer ingreso)");
            if (!usuario.EstaActivo)
                return Rechazar(usuario, "usuario inactivo");

            var tokenContrasena = _emisor.CambioContrasena(usuario.CodUsu);
            _log.LogInformation("Primer ingreso de {wsCodUsu}: debe crear contraseña web", usuario.CodUsu);
            return Resultado<IniciarSesionRespuesta>.Ok(new IniciarSesionRespuesta
            {
                DebeCrearContrasena = true,
                TokenContrasena = tokenContrasena.Token,
                ExpiraEn = SegundosHasta(tokenContrasena.Expira),
            });
        }

        // 3. Ingreso normal: verificar el hash de la contraseña web
        var verificacion = _hash.Verificar(usuario.CodUsu, usuario.PasWbh!, wsPasUsu);
        if (verificacion == PasswordVerificationResult.Failed)
            return Rechazar(usuario, "contraseña web incorrecta");
        if (!usuario.EstaActivo)
            return Rechazar(usuario, "usuario inactivo");

        if (verificacion == PasswordVerificationResult.SuccessRehashNeeded)
        {
            var nuevoHash = _hash.Calcular(usuario.CodUsu, wsPasUsu);
            await _repo.GuardarHashWeb(usuario.CodUsu, nuevoHash, ct);
            usuario = usuario with { PasWbh = nuevoHash };
        }

        // 4. Éxito: token de acceso + token de renovación (este último va en cookie)
        var acceso = _emisor.Acceso(usuario.CodUsu, usuario.NomUsu);
        var renovacion = _emisor.Renovacion(usuario.CodUsu, EmisorTokens.Sello(usuario.PasWbh, usuario.EstadoUsu));
        _log.LogInformation("Login correcto de {wsCodUsu}", usuario.CodUsu);

        return Resultado<IniciarSesionRespuesta>.Ok(new IniciarSesionRespuesta
        {
            TokenAcceso = acceso.Token,
            ExpiraEn = SegundosHasta(acceso.Expira),
            Usuario = new UsuarioSesion(usuario.CodUsu, usuario.NomUsu, usuario.EsMaestro),
            TokenRenovacion = renovacion,
        });
    }

    /// <summary>Registra el motivo real en el log y devuelve siempre el mismo error genérico.</summary>
    private Resultado<IniciarSesionRespuesta> Rechazar(UsuarioAcceso usuario, string motivo)
    {
        _log.LogInformation("Login rechazado de {wsCodUsu}: {Motivo}", usuario.CodUsu, motivo);
        return Resultado<IniciarSesionRespuesta>.Fallo(ErroresAcceso.CredencialesInvalidas);
    }

    /// <summary>
    /// Compara con US1_CPASSWO distinguiendo mayúsculas (decisión del login) y en tiempo constante.
    /// Se recortan espacios como hacía el VB6 (Trim).
    /// </summary>
    private static bool IgualContrasenaVb6(string wsPasUsuGuardada, string wsPasUsuEscrita) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(wsPasUsuGuardada.Trim()),
            Encoding.UTF8.GetBytes(wsPasUsuEscrita.Trim()));

    private static int SegundosHasta(DateTimeOffset expira) =>
        (int)Math.Round((expira - DateTimeOffset.UtcNow).TotalSeconds);
}
