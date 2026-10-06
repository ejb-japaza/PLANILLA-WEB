using Microsoft.AspNetCore.Identity;

namespace Ejb.Planilla.Api.Modulos.Acceso;

/// <summary>Calcula y verifica el hash de la contraseña web (US1_CPASWBH).</summary>
public sealed class HashContrasena
{
    private readonly PasswordHasher<string> _hasher = new();

    /// <summary>Calcula el hash para guardar en US1_CPASWBH.</summary>
    public string Calcular(string wsCodUsu, string wsPasUsu) => _hasher.HashPassword(wsCodUsu, wsPasUsu);

    /// <summary>Verifica la contraseña escrita contra el hash guardado.</summary>
    public PasswordVerificationResult Verificar(string wsCodUsu, string hash, string wsPasUsu) => _hasher.VerifyHashedPassword(wsCodUsu, hash, wsPasUsu);
}
