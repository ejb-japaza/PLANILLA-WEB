using Ejb.Planilla.Api.Compartido.Datos;

namespace Ejb.Planilla.Pruebas.Compartido;

/// <summary>Tabla.Cia: misma regla del VB6 y rechazo de nombres que podrían inyectar SQL.</summary>
public class TablaPruebas
{
    [Theory]
    [InlineData("PLAPER1", "0001", "[PLA0001PER1]")]
    [InlineData("PLATRA1", "0002", "[PLA0002TRA1]")]
    [InlineData("GENTGE1", "0001", "[GEN0001TGE1]")]
    public void Arma_el_nombre_como_el_VB6(string tabla, string wsCodCom, string esperado)
    {
        Tabla.Cia(tabla, wsCodCom).ShouldBe(esperado);
    }

    [Theory]
    [InlineData("PLA1]; DROP TABLE GENUSU1--")]
    [InlineData("plaper1")]
    [InlineData("PL")]
    [InlineData("")]
    public void Rechaza_nombres_de_tabla_no_validos(string tabla)
    {
        Should.Throw<ArgumentException>(() => Tabla.Cia(tabla, "0001"));
    }

    [Theory]
    [InlineData("001")]
    [InlineData("00001")]
    [InlineData("ABCD")]
    [InlineData("0001]--")]
    public void Rechaza_codigos_de_compania_no_validos(string wsCodCom)
    {
        Should.Throw<ArgumentException>(() => Tabla.Cia("PLAPER1", wsCodCom));
    }
}
