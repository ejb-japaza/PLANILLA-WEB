namespace Ejb.Planilla.Api.Compartido;

/// <summary>Error de negocio: código para el programa y mensaje para el usuario.</summary>
public sealed record Falla(string Codigo, string Mensaje);

/// <summary>Resultado de un caso de uso: éxito con valor, o error de negocio. Se crea con Ok o Fallo.</summary>
public sealed class Resultado<T>
{
    public bool Exito { get; }
    public T? Valor { get; }
    public Falla? Error { get; }

    private Resultado(bool exito, T? valor, Falla? error)
    {
        Exito = exito;
        Valor = valor;
        Error = error;
    }

    /// <summary>Resultado correcto con su valor.</summary>
    public static Resultado<T> Ok(T valor) => new(true, valor, null);

    /// <summary>Resultado con error de negocio.</summary>
    public static Resultado<T> Fallo(Falla error) => new(false, default, error);
}
