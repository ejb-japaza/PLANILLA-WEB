namespace Ejb.Planilla.Api.Modulos.Acceso;

/// <summary>
/// Cookie HttpOnly donde viaja el token de renovación. Solo el navegador la envía, y solo a la ruta de renovar.
/// La usan IniciarSesion y CrearContrasenaWeb (y después RenovarSesion / CerrarSesion).
/// </summary>
public static class CookieRenovacion
{
    public const string Nombre = "planilla_renovar";
    public const string Ruta = "/api/acceso/renovar";

    public static void Escribir(HttpResponse response, TokenEmitido token) =>
        response.Cookies.Append(Nombre, token.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = Ruta,
            Expires = token.Expira,
        });
}
