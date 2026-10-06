using Dapper;
using Ejb.Planilla.Api.Compartido.Datos;

namespace Ejb.Planilla.Api.Modulos.Acceso;

/// <summary>Lectura y escritura de GENUSU1 para el login.</summary>
public interface IUsuarioRepo
{
    Task<UsuarioAcceso?> Buscar(string wsCodUsu, CancellationToken ct);
    Task GuardarHashWeb(string wsCodUsu, string hash, CancellationToken ct);
}

/// <summary>Implementación con Dapper sobre la base principal.</summary>
public sealed class UsuarioRepo : IUsuarioRepo
{
    private readonly ICn _cn;

    public UsuarioRepo(ICn cn)
    {
        _cn = cn;
    }

    /// <summary>Busca el usuario por código; null si no existe.</summary>
    public async Task<UsuarioAcceso?> Buscar(string wsCodUsu, CancellationToken ct)
    {
        const string sql = """
            SELECT RTRIM(US1_CCODUSU) AS CodUsu,
                   RTRIM(US1_CNOMUSU) AS NomUsu,
                   US1_CTIPUSU        AS TipUsu,
                   US1_CESTADO        AS EstadoUsu,
                   RTRIM(US1_CPASSWO) AS PasUsu,
                   US1_CPASWBH        AS PasWbh
            FROM GENUSU1
            WHERE US1_CCODUSU = @wsCodUsu
            """;

        using var cn = _cn.Principal();
        return await cn.QuerySingleOrDefaultAsync<UsuarioAcceso>(new CommandDefinition(
            sql,
            new { wsCodUsu = new DbString { Value = wsCodUsu, IsAnsi = true, IsFixedLength = true, Length = 5 } },
            cancellationToken: ct));
    }

    /// <summary>Guarda el hash de la contraseña web en US1_CPASWBH (única columna que escribe el backend).</summary>
    public async Task GuardarHashWeb(string wsCodUsu, string hash, CancellationToken ct)
    {
        const string sql = """
            UPDATE GENUSU1
            SET US1_CPASWBH = @hash
            WHERE US1_CCODUSU = @wsCodUsu
            """;

        using var cn = _cn.Principal();
        await cn.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                wsCodUsu = new DbString { Value = wsCodUsu, IsAnsi = true, IsFixedLength = true, Length = 5 },
                hash = new DbString { Value = hash, IsAnsi = true, Length = 256 },
            },
            cancellationToken: ct));
    }
}