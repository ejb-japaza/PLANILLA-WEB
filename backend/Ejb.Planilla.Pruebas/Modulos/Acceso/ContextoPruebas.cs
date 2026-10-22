using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Dapper;
using Ejb.Planilla.Api.Compartido.Datos;
using Ejb.Planilla.Api.Modulos.Acceso;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Ejb.Planilla.Pruebas.Modulos.Acceso;

/// <summary>
/// Fase 2 del login contra la base de desarrollo (User Secrets → EJBBENNY).
/// Los tokens se generan directamente (no hacen falta contraseñas) y nada se escribe en la base.
/// Lo esperado se calcula con consultas a la base, así las pruebas siguen valiendo si cambian los datos.
/// </summary>
public class ContextoPruebas(WebApplicationFactory<Program> fabrica) : IClassFixture<WebApplicationFactory<Program>>
{
    private CancellationToken Ct => TestContext.Current.CancellationToken;

    private HttpClient Cliente(string? token)
    {
        var c = fabrica.CreateClient();
        if (token is not null) c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    private async Task<T?> Dato<T>(string sql, object? parametros = null)
    {
        using var cn = fabrica.Services.GetRequiredService<ICn>().Principal();
        return await cn.ExecuteScalarAsync<T>(sql, parametros);
    }

    /// <summary>Token de la fase 1 (sin contexto) para un usuario activo del tipo indicado ("M" o no "M").</summary>
    private async Task<(string WsCodUsu, string Token)> TokenFase1(bool maestro)
    {
        var wsCodUsu = await Dato<string>(maestro
            ? "SELECT TOP 1 RTRIM(US1_CCODUSU) FROM GENUSU1 WHERE US1_CESTADO = '1' AND US1_CTIPUSU = 'M' ORDER BY 1"
            : "SELECT TOP 1 RTRIM(US1_CCODUSU) FROM GENUSU1 WHERE US1_CESTADO = '1' AND US1_CTIPUSU <> 'M' ORDER BY 1");
        Assert.SkipWhen(wsCodUsu is null, $"No hay usuario activo {(maestro ? "maestro" : "no maestro")} en la base");
        var emisor = fabrica.Services.GetRequiredService<EmisorTokens>();
        return (wsCodUsu!, emisor.Acceso(wsCodUsu!, "Prueba").Token);
    }

    [Fact]
    public async Task Opciones_sin_token_responde_401()
    {
        var r = await Cliente(null).GetAsync("/api/acceso/contexto/opciones", Ct);
        r.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Usuario_maestro_ve_todas_las_companias_vigentes()
    {
        var (_, token) = await TokenFase1(maestro: true);
        var esperadas = await Dato<int>("SELECT COUNT(*) FROM GENCOM1 WHERE ISNULL(CO1_CESTADO, '') <> 'B'");

        var r = await Cliente(token).GetAsync("/api/acceso/contexto/opciones", Ct);
        r.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = JsonDocument.Parse(await r.Content.ReadAsStringAsync(Ct)).RootElement;

        json.GetProperty("companias").GetArrayLength().ShouldBe(esperadas);
        json.GetProperty("tiposPlanilla").GetArrayLength().ShouldBe(2);
    }

    [Fact]
    public async Task Usuario_no_maestro_ve_solo_las_companias_de_SEGAXU1()
    {
        var (wsCodUsu, token) = await TokenFase1(maestro: false);
        var esperadas = await Dato<int>("""
            SELECT COUNT(DISTINCT CO.CO1_CCODCOM) FROM SEGAXU1 AX
            INNER JOIN GENCOM1 CO ON CO.CO1_CCODCOM = AX.AX1_CCODCOM
            WHERE AX.AX1_CCODUSU = @u AND ISNULL(CO.CO1_CESTADO, '') <> 'B'
            """, new { u = wsCodUsu });

        var r = await Cliente(token).GetAsync("/api/acceso/contexto/opciones", Ct);

        if (esperadas == 0)
        {
            r.StatusCode.ShouldBe(HttpStatusCode.Forbidden); // "no cuenta con una compañía asignada"
            return;
        }
        r.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = JsonDocument.Parse(await r.Content.ReadAsStringAsync(Ct)).RootElement;
        json.GetProperty("companias").GetArrayLength().ShouldBe(esperadas);
    }

    [Fact]
    public async Task Elegir_contexto_da_token_con_contexto_y_periodo_activo()
    {
        var (wsCodUsu, token) = await TokenFase1(maestro: true);

        // 1. Opciones → primera compañía y su tipo por defecto
        var opciones = JsonDocument.Parse(await (await Cliente(token).GetAsync("/api/acceso/contexto/opciones", Ct))
            .Content.ReadAsStringAsync(Ct)).RootElement;
        var cia = opciones.GetProperty("companias")[0];
        var wsCodCom = cia.GetProperty("codCom").GetString()!;

        // 2. Elegir
        var r = await Cliente(token).PostAsJsonAsync("/api/acceso/contexto", new { codCom = wsCodCom }, Ct);
        r.StatusCode.ShouldBe(HttpStatusCode.OK);
        var resp = JsonDocument.Parse(await r.Content.ReadAsStringAsync(Ct)).RootElement;
        var contexto = resp.GetProperty("contexto");
        contexto.GetProperty("codCom").GetString().ShouldBe(wsCodCom);

        // 3. El periodo es el activo (PE1_CSITUAC = 'A'), como fExtPerAct
        var activo = await Dato<string>(
            $"SELECT TOP 1 RTRIM(PE1_CCODPER) FROM {Tabla.Cia("PLAPER1", wsCodCom)} WHERE PE1_CSITUAC = 'A' AND PE1_CTIPTRA = @t AND PE1_CCODSUC = @s",
            new { t = contexto.GetProperty("tipTra").GetString(), s = contexto.GetProperty("codSuc").GetString() });
        if (activo is not null)
            contexto.GetProperty("perAct").GetString().ShouldBe(activo);

        // 4. El token con contexto pasa la política ConContexto
        var actual = await Cliente(resp.GetProperty("tokenAcceso").GetString()).GetAsync("/api/acceso/contexto", Ct);
        actual.StatusCode.ShouldBe(HttpStatusCode.OK);
        var actualJson = JsonDocument.Parse(await actual.Content.ReadAsStringAsync(Ct)).RootElement;
        actualJson.GetProperty("codUsu").GetString().ShouldBe(wsCodUsu);
        actualJson.GetProperty("codCom").GetString().ShouldBe(wsCodCom);
    }

    [Fact]
    public async Task Token_sin_contexto_no_pasa_ConContexto_403()
    {
        var (_, token) = await TokenFase1(maestro: true);
        var r = await Cliente(token).GetAsync("/api/acceso/contexto", Ct);
        r.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Compania_no_permitida_responde_403()
    {
        var (_, token) = await TokenFase1(maestro: true);
        var r = await Cliente(token).PostAsJsonAsync("/api/acceso/contexto", new { codCom = "9999" }, Ct);
        r.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("AB", null)]
    [InlineData("0001", "X")]
    public async Task Datos_con_formato_no_valido_responden_400(string codCom, string? tipTra)
    {
        var (_, token) = await TokenFase1(maestro: true);
        var r = await Cliente(token).PostAsJsonAsync("/api/acceso/contexto", new { codCom, tipTra }, Ct);
        r.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
