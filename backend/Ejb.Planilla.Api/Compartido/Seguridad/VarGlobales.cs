using System.Security.Claims;

namespace Ejb.Planilla.Api.Compartido.Seguridad;

/// <summary>
/// Variables globales del ingreso (las "Global ws..." de GENINI01.bas en el VB6).
/// En la web no viven en memoria: viajan dentro del token de cada petición.
/// </summary>
public static class VarGlobales
{
    // Nombres con que se guardan dentro del token
    public const string CodUsu = "sub";
    public const string NomUsu = "name";
    public const string CodCom = "cia";
    public const string CodSuc = "suc";
    public const string TipTra = "tpl";
    public const string PerAct = "per";

    // Lectura desde el token

    /// <summary>wsCodUsu: código de usuario (siempre presente en un token válido).</summary>
    public static string WsCodUsu(this ClaimsPrincipal usuario) =>
        usuario.FindFirst(CodUsu)!.Value;

    /// <summary>wsCodCom: compañía elegida; null si aún no se eligió contexto.</summary>
    public static string? WsCodCom(this ClaimsPrincipal usuario) =>
        usuario.FindFirst(CodCom)?.Value;

    /// <summary>wsCodSuc: sucursal elegida.</summary>
    public static string? WsCodSuc(this ClaimsPrincipal usuario) =>
        usuario.FindFirst(CodSuc)?.Value;

    /// <summary>wsTipTra: tipo de planilla (E empleados / O obreros).</summary>
    public static string? WsTipTra(this ClaimsPrincipal usuario) =>
        usuario.FindFirst(TipTra)?.Value;

    /// <summary>wsPerAct: periodo actual (automático, como fExtPerAct del VB6).</summary>
    public static string? WsPerAct(this ClaimsPrincipal usuario) =>
        usuario.FindFirst(PerAct)?.Value;
}
