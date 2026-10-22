using Ejb.Planilla.Api.Compartido;

namespace Ejb.Planilla.Api.Modulos.Acceso.ElegirContexto;

/// <summary>
/// Fase 2 del login (GENPAS01 del VB6): qué compañía, sucursal y tipo de planilla puede elegir el usuario,
/// y el periodo actual (fExtPerAct). Toda elección se vuelve a validar en el servidor.
/// </summary>
public sealed class ElegirContextoServicio
{
    private static readonly IReadOnlyList<TipoPlanilla> Tipos =
    [
        new("E", "Empleados"),
        new("O", "Obreros"),
    ];

    private readonly IUsuarioRepo _usuarios;
    private readonly IContextoRepo _repo;
    private readonly EmisorTokens _emisor;
    private readonly ILogger<ElegirContextoServicio> _log;

    public ElegirContextoServicio(IUsuarioRepo usuarios, IContextoRepo repo, EmisorTokens emisor, ILogger<ElegirContextoServicio> log)
    {
        _usuarios = usuarios;
        _repo = repo;
        _emisor = emisor;
        _log = log;
    }

    /// <summary>Compañías permitidas (con sus sucursales, valores por defecto y qué se puede elegir).</summary>
    public async Task<Resultado<OpcionesContexto>> Opciones(string wsCodUsu, CancellationToken ct)
    {
        var usuario = await _usuarios.Buscar(wsCodUsu, ct);
        if (usuario is null || !usuario.EstaActivo)
            return Resultado<OpcionesContexto>.Fallo(ErroresAcceso.UsuarioNoValido);

        var opciones = await ArmarOpciones(usuario, ct);
        if (opciones.Count == 0)
        {
            _log.LogInformation("{wsCodUsu} no tiene compañías asignadas", wsCodUsu);
            return Resultado<OpcionesContexto>.Fallo(ErroresAcceso.SinCompania);
        }

        return Resultado<OpcionesContexto>.Ok(new OpcionesContexto(opciones, Tipos));
    }

    /// <summary>Valida la elección y emite el token de acceso con contexto.</summary>
    public async Task<Resultado<ElegirContextoRespuesta>> Elegir(string wsCodUsu, ElegirContextoSolicitud solicitud, CancellationToken ct)
    {
        // 1. El usuario sigue existiendo y activo
        var usuario = await _usuarios.Buscar(wsCodUsu, ct);
        if (usuario is null || !usuario.EstaActivo)
            return Resultado<ElegirContextoRespuesta>.Fallo(ErroresAcceso.UsuarioNoValido);

        // 2. La compañía está entre las permitidas
        var companias = await ArmarOpciones(usuario, ct);
        var cia = companias.FirstOrDefault(c => c.CodCom == solicitud.CodCom);
        if (cia is null)
            return Rechazar(wsCodUsu, ErroresAcceso.ContextoNoPermitido, $"compañía {solicitud.CodCom} no permitida");

        // 3. Sucursal: la pedida (si se puede elegir y está permitida) o la primera, como el VB6
        if (cia.Sucursales.Count == 0)
            return Rechazar(wsCodUsu, ErroresAcceso.SinSucursal, $"sin sucursales en {cia.CodCom}");

        var suc = cia.PuedeElegirSucursal && !string.IsNullOrWhiteSpace(solicitud.CodSuc)
            ? cia.Sucursales.FirstOrDefault(s => s.CodSuc == solicitud.CodSuc)
            : cia.Sucursales[0];
        if (suc is null)
            return Rechazar(wsCodUsu, ErroresAcceso.ContextoNoPermitido, $"sucursal {solicitud.CodSuc} no permitida en {cia.CodCom}");

        // 4. Tipo de planilla: el pedido (si se puede elegir) o el por defecto (CO1_CTPLDEF / VPQUINCE)
        var wsTipTra = cia.PuedeElegirTipTra && solicitud.TipTra is not null ? solicitud.TipTra : cia.TipTraDefecto;

        // 5. Periodo automático (fExtPerAct)
        var wsPerAct = await _repo.PeriodoActual(cia.CodCom, wsTipTra, suc.CodSuc, ct);
        if (wsPerAct is null)
            return Rechazar(wsCodUsu, ErroresAcceso.SinPeriodo, $"sin periodos en {cia.CodCom}/{suc.CodSuc}/{wsTipTra}");

        // 6. Token de acceso con contexto
        var token = _emisor.Acceso(usuario.CodUsu, usuario.NomUsu, new Contexto(cia.CodCom, suc.CodSuc, wsTipTra, wsPerAct));
        _log.LogInformation("{wsCodUsu} eligió contexto {wsCodCom}/{wsCodSuc}/{wsTipTra}/{wsPerAct}",
            usuario.CodUsu, cia.CodCom, suc.CodSuc, wsTipTra, wsPerAct);

        return Resultado<ElegirContextoRespuesta>.Ok(new ElegirContextoRespuesta(
            token.Token,
            (int)Math.Round((token.Expira - DateTimeOffset.UtcNow).TotalSeconds),
            new ContextoActual(cia.CodCom, cia.NomCom, suc.CodSuc, suc.NomSuc, wsTipTra, wsPerAct)));
    }

    /// <summary>Compañías permitidas con sus sucursales y reglas (decisión: indicadores de la compañía elegida, no de la 0001).</summary>
    private async Task<List<CompaniaOpcion>> ArmarOpciones(UsuarioAcceso usuario, CancellationToken ct)
    {
        var resultado = new List<CompaniaOpcion>();
        foreach (var c in await _repo.Companias(usuario.CodUsu, usuario.EsMaestro, ct))
        {
            var sucursales = await _repo.Sucursales(usuario.CodUsu, usuario.EsMaestro, c.CodCom, ct);
            var vpQuince = await _repo.VpQuince(c.CodCom, ct);

            // fVerTipoPlanilla: CO1_CTPLDEF "O" → O; "A" o "E" → E. VPQUINCE = 'S' fuerza O.
            var tipTraDefecto = vpQuince || c.TplDef == "O" ? "O" : "E";

            resultado.Add(new CompaniaOpcion(
                c.CodCom,
                c.NomCom,
                tipTraDefecto,
                PuedeElegirSucursal: c.HabSuc == "S",
                PuedeElegirTipTra: c.HabTip == "S" && !vpQuince,
                sucursales));
        }
        return resultado;
    }

    private Resultado<ElegirContextoRespuesta> Rechazar(string wsCodUsu, Falla falla, string motivo)
    {
        _log.LogInformation("Contexto rechazado para {wsCodUsu}: {Motivo}", wsCodUsu, motivo);
        return Resultado<ElegirContextoRespuesta>.Fallo(falla);
    }
}
