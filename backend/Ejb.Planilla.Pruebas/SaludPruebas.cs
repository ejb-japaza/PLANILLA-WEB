using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Ejb.Planilla.Pruebas;

/// <summary>
/// Levanta la API en memoria con la configuración de desarrollo (User Secrets)
/// y comprueba que arranca, se conecta a la base y tiene los cambios de BD aplicados.
/// </summary>
public class SaludPruebas(WebApplicationFactory<Program> fabrica) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task La_API_arranca_y_se_conecta_a_la_base()
    {
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.GetAsync("/salud", TestContext.Current.CancellationToken);
        var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement;

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
        json.GetProperty("estado").GetString().ShouldBe("ok");
        json.GetProperty("baseDatos").GetString()!.ShouldStartWith("Conectado a");
        json.GetProperty("cambiosBd").GetString().ShouldBe("Completos");
    }
}
