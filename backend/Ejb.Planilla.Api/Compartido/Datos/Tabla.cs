using System.Text.RegularExpressions;

namespace Ejb.Planilla.Api.Compartido.Datos;

/// <summary>Nombres de tablas por compañía, con la misma regla del VB6 ("PLA" + wsCodCom + "PER1").</summary>
public static class Tabla
{
    /// <summary>"PLAPER1" + "0001" → "[PLA0001PER1]". Lanza error si los datos no tienen el formato esperado.</summary>
    public static string Cia(string tabla, string wsCodCom)
    {
        // Validar la tabla
        if (!Regex.IsMatch(tabla, "^[A-Z]{3}[A-Z0-9]{2,6}$"))
            throw new ArgumentException($"Nombre de tabla no válido: {tabla}", nameof(tabla));

        // Validar la compañía: exactamente 4 dígitos (0001)
        if (!Regex.IsMatch(wsCodCom, "^[0-9]{4}$"))
            throw new ArgumentException($"Código de compañía no válido: {wsCodCom}", nameof(wsCodCom));

        // Armar y devolver el nombre
        return $"[{tabla[..3]}{wsCodCom}{tabla[3..]}]";
    }
}