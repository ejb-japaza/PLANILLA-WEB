namespace Ejb.Planilla.Api.Modulos.Acceso.ElegirContexto;

// ---------- GET /api/acceso/contexto/opciones ----------

/// <summary>Lo que el usuario puede elegir: compañías (con sus sucursales) y tipos de planilla.</summary>
public sealed record OpcionesContexto(IReadOnlyList<CompaniaOpcion> Companias, IReadOnlyList<TipoPlanilla> TiposPlanilla);

/// <summary>Una compañía permitida, con lo que el frontend necesita para armar los combos.</summary>
public sealed record CompaniaOpcion(
    string CodCom,
    string NomCom,
    string TipTraDefecto,
    bool PuedeElegirSucursal,
    bool PuedeElegirTipTra,
    IReadOnlyList<SucursalPermitida> Sucursales);

/// <summary>Tipo de planilla: E empleados / O obreros.</summary>
public sealed record TipoPlanilla(string TipTra, string Nombre);

// ---------- POST /api/acceso/contexto ----------

/// <summary>
/// Lo que elige el usuario. CodSuc y TipTra son opcionales: si la compañía no permite elegirlos
/// (PL1_CHABSUC / PL1_CHABTIP), el servidor usa el valor por defecto, como el VB6 con el combo deshabilitado.
/// </summary>
public sealed record ElegirContextoSolicitud(string CodCom, string? CodSuc, string? TipTra);

/// <summary>Token de acceso CON contexto y los datos elegidos (con nombres, para mostrarlos en pantalla).</summary>
public sealed record ElegirContextoRespuesta(string TokenAcceso, int ExpiraEn, ContextoActual Contexto);

/// <summary>Contexto de trabajo: wsCodCom, wsCodSuc, wsTipTra, wsPerAct del VB6.</summary>
public sealed record ContextoActual(string CodCom, string NomCom, string CodSuc, string NomSuc, string TipTra, string PerAct);
