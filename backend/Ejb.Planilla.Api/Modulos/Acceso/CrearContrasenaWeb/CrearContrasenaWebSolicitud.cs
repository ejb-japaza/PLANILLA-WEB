namespace Ejb.Planilla.Api.Modulos.Acceso.CrearContrasenaWeb;

/// <summary>Contraseña web nueva (primer ingreso). El usuario sale del token, no del cuerpo.</summary>
public sealed record CrearContrasenaWebSolicitud(string Nueva, string Confirmacion);
