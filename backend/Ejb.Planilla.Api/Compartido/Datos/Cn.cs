using Microsoft.Data.SqlClient;

namespace Ejb.Planilla.Api.Compartido.Datos;

/// <summary>
/// Equivale a la conexión global "Cn" del VB6, pero en la web se abre una conexión nueva
/// por consulta (muchos usuarios a la vez). Usar siempre con: using var cn = _cn.Principal();
/// </summary>
public interface ICn
{
    /// <summary>Conexión a la base principal del cliente (la de ejb.com en el VB6).</summary>
    SqlConnection Principal();
}

/// <summary>Crea las conexiones con la cadena de "ConnectionStrings:Principal" (User Secrets / web.config).</summary>
public sealed class Cn : ICn
{
    private readonly string _cadenaPrincipal;

    public Cn(IConfiguration configuracion)
    {
        var cadena = configuracion.GetConnectionString("Principal");
        if (string.IsNullOrWhiteSpace(cadena))
            throw new InvalidOperationException(
                "Falta 'ConnectionStrings:Principal' (User Secrets en desarrollo, variable de entorno en producción).");
        _cadenaPrincipal = cadena;
    }

    public SqlConnection Principal() => new(_cadenaPrincipal);
}
