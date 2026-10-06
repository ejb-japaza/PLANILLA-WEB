using Dapper;
using Ejb.Planilla.Api.Compartido.Datos;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ejb.Planilla.Api.Compartido.Salud;

/// <summary>Comprueba la conexión a la base principal.</summary>
public sealed class SaludBaseDatos(ICn _cn) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var cn = _cn.Principal();
            await cn.ExecuteScalarAsync<int>(new CommandDefinition("SELECT 1", cancellationToken: cancellationToken));
            return HealthCheckResult.Healthy($"Conectado a {cn.Database}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Sin conexión a la base principal", ex);
        }
    }
}

/// <summary>
/// Comprueba que existan los cambios de base de datos aprobados (scripts de db/cambios).
/// El backend NO los crea: solo avisa si faltan.
/// </summary>
public sealed class SaludCambiosBd(ICn _cn) : IHealthCheck
{
    private static readonly (string Tabla, string Columna, string Script)[] Requeridos =
    [
        ("GENUSU1", "US1_CPASWBH", "001_GENUSU1_US1_CPASWBH.sql"),
    ];

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var cn = _cn.Principal();
            var faltantes = new List<string>();
            foreach (var (tabla, columna, script) in Requeridos)
            {
                var existe = await cn.ExecuteScalarAsync<int?>(new CommandDefinition(
                    "SELECT COL_LENGTH(@tabla, @columna)", new { tabla = "dbo." + tabla, columna }, cancellationToken: cancellationToken));
                if (existe is null)
                    faltantes.Add($"{tabla}.{columna} (ejecutar db/cambios/{script})");
            }

            return faltantes.Count == 0
                ? HealthCheckResult.Healthy("Completos")
                : HealthCheckResult.Unhealthy("Falta " + string.Join("; ", faltantes));
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("No se pudo verificar el esquema", ex);
        }
    }
}
