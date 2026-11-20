using BudgetCivil.Application.Dtos;
using FluentValidation;

namespace BudgetCivil.Application.Validators;

public sealed class ChangeMyPasswordRequestValidator : AbstractValidator<ChangeMyPasswordRequest>
{
    public ChangeMyPasswordRequestValidator()
    {
        RuleFor(request => request.CurrentPassword)
            .NotEmpty();

        RuleFor(request => request.NewPassword)
            .NotEmpty()
            .WithMessage("La contraseña es obligatoria.")
            .MinimumLength(8)
            .WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .Matches(@"\p{Lu}")
            .WithMessage("La contraseña debe incluir al menos una mayúscula.")
            .Matches(@"\p{Nd}")
            .WithMessage("La contraseña debe incluir al menos un número.")
            .Matches(@"[^\p{L}\p{N}\s]")
            .WithMessage("La contraseña debe incluir al menos un símbolo.");
    }
}
