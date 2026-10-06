using FluentValidation;

namespace Ejb.Planilla.Api.Modulos.Acceso.CrearContrasenaWeb;

/// <summary>Sin reglas de complejidad por ahora (decisión del login): no vacía, largo máximo y confirmación igual.</summary>
public sealed class CrearContrasenaWebValidador : AbstractValidator<CrearContrasenaWebSolicitud>
{
    public CrearContrasenaWebValidador()
    {
        RuleFor(x => x.Nueva)
            .NotEmpty().WithMessage("Ingrese la contraseña nueva.")
            .MaximumLength(128).WithMessage("La contraseña es demasiado larga.");

        RuleFor(x => x.Confirmacion)
            .Equal(x => x.Nueva).WithMessage("La confirmación no coincide.");
    }
}
