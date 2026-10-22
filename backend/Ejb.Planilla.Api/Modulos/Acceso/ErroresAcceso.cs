using Ejb.Planilla.Api.Compartido;

namespace Ejb.Planilla.Api.Modulos.Acceso;

/// <summary>Errores de negocio del módulo Acceso.</summary>
public static class ErroresAcceso
{
    /// <summary>Mensaje único para cualquier fallo de login (no revela si el usuario existe).</summary>
    public static readonly Falla CredencialesInvalidas = new("Acceso.CredencialesInvalidas", "Usuario o contraseña incorrectos");

    /// <summary>El token de cambio de contraseña ya no sirve (usuario inactivo o la contraseña web ya fue creada).</summary>
    public static readonly Falla ContrasenaNoPermitida = new("Acceso.ContrasenaNoPermitida", "No se puede crear la contraseña. Vuelva a iniciar sesión.");

    // Fase 2 del login (contexto de trabajo)

    /// <summary>El usuario del token ya no existe o fue desactivado.</summary>
    public static readonly Falla UsuarioNoValido = new("Acceso.UsuarioNoValido", "La sesión ya no es válida. Vuelva a iniciar sesión.");

    /// <summary>GENPAS01: "El usuario no cuenta con una compañía asignada".</summary>
    public static readonly Falla SinCompania = new("Acceso.SinCompania", "El usuario no cuenta con una compañía asignada.");

    /// <summary>La compañía o sucursal pedida no está entre las permitidas para el usuario.</summary>
    public static readonly Falla ContextoNoPermitido = new("Acceso.ContextoNoPermitido", "No tiene acceso a la compañía o sucursal seleccionada.");

    /// <summary>GENPAS01: "El usuario no cuenta con una sucursal asignada".</summary>
    public static readonly Falla SinSucursal = new("Acceso.SinSucursal", "El usuario no cuenta con una sucursal asignada en esta compañía.");

    /// <summary>No hay periodos en PLA{cía}PER1 para ese tipo de planilla y sucursal (el login no los genera).</summary>
    public static readonly Falla SinPeriodo = new("Acceso.SinPeriodo", "La compañía no tiene periodos configurados para ese tipo de planilla y sucursal.");
}