using FluentValidation;

namespace Ejb.Planilla.Api.Modulos.Acceso.IniciarSesion;

/// <summary>Valida la forma de los datos antes de consultar la base.</summary>
public sealed class IniciarSesionValidador : AbstractValidator<IniciarSesionSolicitud>
{
    public IniciarSesionValidador()
    {
        RuleFor(x => x.Usuario)
            .NotEmpty().WithMessage("Ingrese el usuario.")
            .MaximumLength(5).WithMessage("El usuario tiene como máximo 5 caracteres.");

        RuleFor(x => x.Contrasena)
            .NotEmpty().WithMessage("Ingrese la contraseña.")
            .MaximumLength(128).WithMessage("La contraseña es demasiado larga.");
    }
}
