using System.Text.Json.Serialization;

namespace Ejb.Planilla.Api.Modulos.Acceso.IniciarSesion;

/// <summary>
/// Respuesta del login. Dos casos:
/// ingreso correcto (TokenAcceso + Usuario) o primer ingreso (DebeCrearContrasena + TokenContrasena).
/// Los campos vacíos no se envían en el JSON.
/// </summary>
public sealed record IniciarSesionRespuesta
{
    public string? TokenAcceso { get; init; }

    /// <summary>Segundos hasta que vence el token entregado (de acceso o de contraseña).</summary>
    public int ExpiraEn { get; init; }

    public UsuarioSesion? Usuario { get; init; }

    public bool DebeCrearContrasena { get; init; }

    public string? TokenContrasena { get; init; }

    /// <summary>Va en una cookie HttpOnly, nunca en el JSON.</summary>
    [JsonIgnore]
    public TokenEmitido? TokenRenovacion { get; init; }
}

/// <summary>Datos del usuario que el frontend muestra tras ingresar.</summary>
public sealed record UsuarioSesion(string CodUsu, string NomUsu, bool EsMaestro);
