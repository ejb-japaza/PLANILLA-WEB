using Dapper;
using Ejb.Planilla.Api.Compartido.Datos;

namespace Ejb.Planilla.Api.Modulos.Acceso;

/// <summary>Compañía a la que el usuario puede entrar (GENCOM1), con sus indicadores.</summary>
public sealed record CompaniaPermitida(string CodCom, string NomCom, string TplDef, string HabSuc, string HabTip);

/// <summary>Sucursal a la que el usuario puede entrar (GENSUC1).</summary>
public sealed record SucursalPermitida(string CodSuc, string NomSuc);

/// <summary>Consultas de la fase 2 del login: las de GENPAS01 (compañías, sucursales) y fExtPerAct (periodo) del VB6.</summary>
public interface IContextoRepo
{
    Task<IReadOnlyList<CompaniaPermitida>> Companias(string wsCodUsu, bool esMaestro, CancellationToken ct);
    Task<IReadOnlyList<SucursalPermitida>> Sucursales(string wsCodUsu, bool esMaestro, string wsCodCom, CancellationToken ct);
    Task<bool> VpQuince(string wsCodCom, CancellationToken ct);
    Task<string?> PeriodoActual(string wsCodCom, string wsTipTra, string wsCodSuc, CancellationToken ct);
}

/// <summary>Implementación con Dapper sobre la base principal.</summary>
public sealed class ContextoRepo : IContextoRepo
{
    private readonly ICn _cn;

    public ContextoRepo(ICn cn)
    {
        _cn = cn;
    }

    /// <summary>
    /// Compañías permitidas (GENPAS01.fValAcceso): usuario "M" → todas las vigentes;
    /// los demás → las asignadas en SEGAXU1. En ambos casos sin las dadas de baja (CO1_CESTADO = 'B').
    /// </summary>
    public async Task<IReadOnlyList<CompaniaPermitida>> Companias(string wsCodUsu, bool esMaestro, CancellationToken ct)
    {
        const string columnas = """
            RTRIM(CO.CO1_CCODCOM)              AS CodCom,
            RTRIM(ISNULL(CO.CO1_CNOMCIA, ''))  AS NomCom,
            RTRIM(ISNULL(CO.CO1_CTPLDEF, ''))  AS TplDef,
            RTRIM(ISNULL(CO.PL1_CHABSUC, ''))  AS HabSuc,
            RTRIM(ISNULL(CO.PL1_CHABTIP, ''))  AS HabTip
            """;

        var sql = esMaestro
            ? $"""
               SELECT {columnas}
               FROM GENCOM1 AS CO
               WHERE ISNULL(CO.CO1_CESTADO, '') <> 'B'
               ORDER BY CO.CO1_CCODCOM
               """
            : $"""
               SELECT DISTINCT {columnas}
               FROM SEGAXU1 AS AX
               INNER JOIN GENCOM1 AS CO ON CO.CO1_CCODCOM = AX.AX1_CCODCOM
               WHERE AX.AX1_CCODUSU = @wsCodUsu
                 AND ISNULL(CO.CO1_CESTADO, '') <> 'B'
               ORDER BY CodCom
               """;

        using var cn = _cn.Principal();
        var filas = await cn.QueryAsync<CompaniaPermitida>(new CommandDefinition(
            sql, new { wsCodUsu = Ansi(wsCodUsu, 10) }, cancellationToken: ct));
        return filas.ToList();
    }

    /// <summary>
    /// Sucursales permitidas de una compañía (GENPAS01.fValAcceso): usuario "M" → todas las de GENSUC1;
    /// los demás → las de SEGAXU1 que existan en GENSUC1.
    /// </summary>
    public async Task<IReadOnlyList<SucursalPermitida>> Sucursales(string wsCodUsu, bool esMaestro, string wsCodCom, CancellationToken ct)
    {
        var sql = esMaestro
            ? """
              SELECT RTRIM(SU.SU1_CCODSUC) AS CodSuc, RTRIM(ISNULL(SU.SU1_CNOMSUC, '')) AS NomSuc
              FROM GENSUC1 AS SU
              WHERE SU.SU1_CCODCOM = @wsCodCom
              ORDER BY SU.SU1_CCODSUC
              """
            : """
              SELECT DISTINCT RTRIM(SU.SU1_CCODSUC) AS CodSuc, RTRIM(ISNULL(SU.SU1_CNOMSUC, '')) AS NomSuc
              FROM SEGAXU1 AS AX
              INNER JOIN GENSUC1 AS SU ON SU.SU1_CCODCOM = AX.AX1_CCODCOM AND SU.SU1_CCODSUC = AX.AX1_CCODSUC
              WHERE AX.AX1_CCODUSU = @wsCodUsu AND AX.AX1_CCODCOM = @wsCodCom
              ORDER BY CodSuc
              """;

        using var cn = _cn.Principal();
        var filas = await cn.QueryAsync<SucursalPermitida>(new CommandDefinition(
            sql, new { wsCodUsu = Ansi(wsCodUsu, 10), wsCodCom = Ansi(wsCodCom, 10) }, cancellationToken: ct));
        return filas.ToList();
    }

    /// <summary>VPQUINCE = 'S' en GEN{cía}TGE1 (grupo P99): fuerza el tipo de planilla "O" (GENPAS01.cmdAceptar).</summary>
    public async Task<bool> VpQuince(string wsCodCom, CancellationToken ct)
    {
        var tabla = Tabla.Cia("GENTGE1", wsCodCom);

        using var cn = _cn.Principal();
        if (!await ExisteTabla(cn, tabla, ct))
            return false;

        var valor = await cn.ExecuteScalarAsync<string?>(new CommandDefinition(
            $"SELECT LEFT(TG1_CDESCRI, 1) FROM {tabla} WHERE TG1_CCODTGE = 'P99' AND TG1_CENLACE = 'VPQUINCE'",
            cancellationToken: ct));
        return valor == "S";
    }

    /// <summary>
    /// Periodo actual (fExtPerAct del VB6): el de PLA{cía}PER1 con situación 'A' para el tipo y la sucursal.
    /// Si no hay activo, el más reciente (el VB6 tomaba "el último encontrado", sin orden). Null si no hay ninguno.
    /// </summary>
    public async Task<string?> PeriodoActual(string wsCodCom, string wsTipTra, string wsCodSuc, CancellationToken ct)
    {
        var tabla = Tabla.Cia("PLAPER1", wsCodCom);

        using var cn = _cn.Principal();
        if (!await ExisteTabla(cn, tabla, ct))
            return null;

        return await cn.ExecuteScalarAsync<string?>(new CommandDefinition(
            $"""
             SELECT TOP 1 RTRIM(PE1_CCODPER)
             FROM {tabla}
             WHERE PE1_CTIPTRA = @wsTipTra AND PE1_CCODSUC = @wsCodSuc
             ORDER BY CASE WHEN PE1_CSITUAC = 'A' THEN 0 ELSE 1 END, PE1_CCODPER DESC
             """,
            new { wsTipTra = Ansi(wsTipTra, 1), wsCodSuc = Ansi(wsCodSuc, 4) },
            cancellationToken: ct));
    }

    private static async Task<bool> ExisteTabla(Microsoft.Data.SqlClient.SqlConnection cn, string tabla, CancellationToken ct) =>
        await cn.ExecuteScalarAsync<int?>(new CommandDefinition(
            "SELECT OBJECT_ID(@tabla, 'U')", new { tabla = Ansi(tabla, 128) }, cancellationToken: ct)) is not null;

    /// <summary>Parámetro de texto como varchar (igual que las columnas char/varchar), para que SQL Server use los índices.</summary>
    private static DbString Ansi(string valor, int largo) =>
        new() { Value = valor, IsAnsi = true, Length = largo };
}
