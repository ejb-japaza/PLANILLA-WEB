using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace Ejb.Planilla.Api.Compartido.Seguridad;

/// <summary>Configura JWT (acceso y cambio de contraseña), la política "todo exige sesión" y el límite de intentos del login.</summary>
public static class ConfiguracionSeguridad
{
    /// <summary>Nombre del límite de intentos: se aplica con [EnableRateLimiting(ConfiguracionSeguridad.LimiteLogin)].</summary>
    public const string LimiteLogin = "login";

    /// <summary>
    /// Esquema que acepta SOLO el token de cambio de contraseña (primer ingreso).
    /// Se aplica con [Authorize(AuthenticationSchemes = ConfiguracionSeguridad.EsquemaContrasena)].
    /// </summary>
    public const string EsquemaContrasena = "Contrasena";

    public static IServiceCollection AddSeguridad(this IServiceCollection services, IConfiguration configuracion)
    {
        var opciones = configuracion.GetSection(OpcionesJwt.Seccion).Get<OpcionesJwt>() ?? new OpcionesJwt();

        var clave = Encoding.UTF8.GetBytes(opciones.ClaveFirma);
        if (clave.Length < 32)
            throw new InvalidOperationException(
                "Falta 'Jwt:ClaveFirma' o tiene menos de 256 bits (User Secrets en desarrollo, variable de entorno Jwt__ClaveFirma en producción).");

        services.Configure<OpcionesJwt>(configuracion.GetSection(OpcionesJwt.Seccion));

        // Dos esquemas JWT: misma clave y emisor, distinta audiencia.
        // Por defecto (todos los endpoints): solo tokens de ACCESO.
        // "Contrasena" (solo POST /api/acceso/contrasena): solo tokens de CAMBIO DE CONTRASEÑA.
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt => Configurar(jwt, opciones.Audiencia))
            .AddJwtBearer(EsquemaContrasena, jwt => Configurar(jwt, opciones.AudienciaContrasena));

        void Configurar(JwtBearerOptions jwt, string audiencia)
        {
            jwt.MapInboundClaims = false; // conservar los nombres cortos de ClaimsPlanilla
            jwt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = opciones.Emisor,
                ValidAudience = audiencia,
                IssuerSigningKey = new SymmetricSecurityKey(clave),
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                ClockSkew = TimeSpan.FromSeconds(30),
                NameClaimType = ClaimsPlanilla.Usuario,
            };
        }

        // Todo endpoint exige sesión salvo los marcados con [AllowAnonymous]
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        // Límite de intentos del login: 5 por minuto por IP (responde 429)
        services.AddRateLimiter(limites =>
        {
            limites.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limites.AddPolicy(LimiteLogin, http => RateLimitPartition.GetFixedWindowLimiter(
                http.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) }));
        });

        return services;
    }
}
