using Ejb.Planilla.Api.Compartido;

namespace Ejb.Planilla.Api.Modulos.Acceso;

/// <summary>Errores de negocio del módulo Acceso.</summary>
public static class ErroresAcceso
{
    /// <summary>Mensaje único para cualquier fallo de login (no revela si el usuario existe).</summary>
    public static readonly Falla CredencialesInvalidas = new("Acceso.CredencialesInvalidas", "Usuario o contraseña incorrectos");

    /// <summary>El token de cambio de contraseña ya no sirve (usuario inactivo o la contraseña web ya fue creada).</summary>
    public static readonly Falla ContrasenaNoPermitida = new("Acceso.ContrasenaNoPermitida", "No se puede crear la contraseña. Vuelva a iniciar sesión.");
}