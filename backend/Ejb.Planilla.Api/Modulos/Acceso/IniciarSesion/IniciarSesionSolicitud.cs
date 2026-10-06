namespace Ejb.Planilla.Api.Modulos.Acceso.IniciarSesion;

/// <summary>Lo que envía el frontend al iniciar sesión.</summary>
public sealed record IniciarSesionSolicitud(string Usuario, string Contrasena);
