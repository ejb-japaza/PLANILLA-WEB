namespace Ejb.Planilla.Api.Compartido.Seguridad;

/// <summary>Sección "Jwt" de la configuración. La clave de firma va en User Secrets / variable de entorno.</summary>
public sealed class OpcionesJwt
{
    public const string Seccion = "Jwt";

    public string Emisor { get; set; } = string.Empty;
    public string Audiencia { get; set; } = string.Empty;
    public string ClaveFirma { get; set; } = string.Empty;
    public int MinutosAcceso { get; set; } = 15;
    public int HorasRenovacion { get; set; } = 10;

    /// <summary>Audiencia del token de renovación (distinta de la de acceso).</summary>
    public string AudienciaRenovacion { get; set; } = string.Empty;

    /// <summary>Audiencia del token de cambio de contraseña (primer ingreso).</summary>
    public string AudienciaContrasena { get; set; } = string.Empty;

    public int MinutosContrasena { get; set; } = 5;
}
