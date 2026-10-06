using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ejb.Planilla.Api.Compartido.Salud;

/// <summary>
/// Respuesta simple de /salud. Ejemplo:
/// { "estado": "ok", "baseDatos": "Conectado a EJBBENNY", "cambiosBd": "Completos" }
/// No incluye detalles de excepciones ni cadenas de conexión.
/// </summary>
public static class RespuestaSalud
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static Task Escribir(HttpContext http, HealthReport reporte)
    {
        http.Response.ContentType = "application/json; charset=utf-8";
        var cuerpo = new
        {
            estado = reporte.Status == HealthStatus.Healthy ? "ok" : "error",
            baseDatos = Detalle(reporte, "base-datos"),
            cambiosBd = Detalle(reporte, "cambios-bd"),
        };
        return http.Response.WriteAsync(JsonSerializer.Serialize(cuerpo, Json));
    }

    private static string? Detalle(HealthReport reporte, string chequeo) =>
        reporte.Entries.TryGetValue(chequeo, out var e) ? e.Description : null;
}
