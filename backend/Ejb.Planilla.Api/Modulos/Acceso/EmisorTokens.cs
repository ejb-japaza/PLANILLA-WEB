using System.Security.Cryptography;
using System.Text;
using Ejb.Planilla.Api.Compartido.Seguridad;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Ejb.Planilla.Api.Modulos.Acceso;

/// <summary>Token firmado y su fecha de expiración.</summary>
public sealed record TokenEmitido(string Token, DateTimeOffset Expira);

/// <summary>Contexto elegido en la fase 2 del login (compañía, sucursal, tipo de planilla, periodo).</summary>
public sealed record Contexto(string CodCom, string CodSuc, string TipTra, string PerAct);

/// <summary>Crea y firma los tokens JWT de acceso, renovación y cambio de contraseña.</summary>
public sealed class EmisorTokens
{
    private readonly OpcionesJwt _opciones;
    private readonly SigningCredentials _firma;
    private readonly JsonWebTokenHandler _manejador = new();

    public EmisorTokens(IOptions<OpcionesJwt> opciones)
    {
        _opciones = opciones.Value;
        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opciones.ClaveFirma));
        _firma = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);
    }

    /// <summary>Arma y firma un token con la audiencia, duración y datos indicados.</summary>
    private TokenEmitido Crear(string audiencia, TimeSpan duracion, Dictionary<string, object> datos)
    {
        var expira = DateTimeOffset.UtcNow.Add(duracion);
        var token = _manejador.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _opciones.Emisor,
            Audience = audiencia,
            Expires = expira.UtcDateTime,
            Claims = datos,
            SigningCredentials = _firma,
        });

        return new TokenEmitido(token, expira);
    }

    /// <summary>Token de acceso (15 min): va en cada petición como Bearer. Con contexto, sirve para los módulos (ConContexto).</summary>
    public TokenEmitido Acceso(string wsCodUsu, string nombreUsuario, Contexto? contexto = null)
    {
        // 1. Datos de siempre
        var datos = new Dictionary<string, object>
        {
            [VarGlobales.CodUsu] = wsCodUsu,
            [VarGlobales.NomUsu] = nombreUsuario,
        };

        // 2. Si hay contexto (fase 2 del login), agregar los 4 datos
        if (contexto is not null)
        {
            datos[VarGlobales.CodCom] = contexto.CodCom;
            datos[VarGlobales.CodSuc] = contexto.CodSuc;
            datos[VarGlobales.TipTra] = contexto.TipTra;
            datos[VarGlobales.PerAct] = contexto.PerAct;
        }

        // 3. Crear el token
        return Crear(_opciones.Audiencia, TimeSpan.FromMinutes(_opciones.MinutosAcceso), datos);
    }

    /// <summary>Token de renovación (10 h): va en la cookie y permite pedir un nuevo token de acceso sin contraseña.</summary>
    public TokenEmitido Renovacion(string wsCodUsu, string sello) =>
        Crear(_opciones.AudienciaRenovacion, TimeSpan.FromHours(_opciones.HorasRenovacion),
            new Dictionary<string, object> { [VarGlobales.CodUsu] = wsCodUsu, ["sello"] = sello });

    /// <summary>Token de cambio de contraseña (5 min): solo para crear la contraseña web en el primer ingreso.</summary>
    public TokenEmitido CambioContrasena(string wsCodUsu) =>
        Crear(_opciones.AudienciaContrasena, TimeSpan.FromMinutes(_opciones.MinutosContrasena),
            new Dictionary<string, object> { [VarGlobales.CodUsu] = wsCodUsu });

    /// <summary>Resumen de la contraseña web y el estado: si alguno cambia, el token de renovación deja de servir.</summary>
    public static string Sello(string? hashWeb, string estado) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{hashWeb}|{estado}")))[..16];
}
