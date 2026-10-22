using FluentValidation;

namespace Ejb.Planilla.Api.Modulos.Acceso.ElegirContexto;

/// <summary>Forma de los datos; si están permitidos para el usuario lo decide el servicio.</summary>
public sealed class ElegirContextoValidador : AbstractValidator<ElegirContextoSolicitud>
{
    public ElegirContextoValidador()
    {
        RuleFor(x => x.CodCom)
            .NotEmpty().WithMessage("Seleccione una compañía.")
            .Matches("^[0-9]{4}$").WithMessage("Código de compañía no válido.");

        RuleFor(x => x.CodSuc)
            .MaximumLength(4).WithMessage("Código de sucursal no válido.");

        RuleFor(x => x.TipTra)
            .Must(t => t is null or "E" or "O").WithMessage("El tipo de planilla debe ser E (empleados) u O (obreros).");
    }
}
