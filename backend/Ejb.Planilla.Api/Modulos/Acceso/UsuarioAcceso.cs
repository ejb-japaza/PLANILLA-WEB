namespace Ejb.Planilla.Api.Modulos.Acceso;

public sealed record UsuarioAcceso(string CodUsu, string NomUsu, string TipUsu, string EstadoUsu, string PasUsu, string? PasWbh)
{
    public bool EstaActivo => EstadoUsu == "1";
    public bool EsMaestro => TipUsu == "M";
    public bool DebeCrearContrasena => string.IsNullOrEmpty(PasWbh);
}