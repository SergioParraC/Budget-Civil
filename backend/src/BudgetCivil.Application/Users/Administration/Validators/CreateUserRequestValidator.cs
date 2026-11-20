using BudgetCivil.Application.Users.Administration.Dtos;
using FluentValidation;

namespace BudgetCivil.Application.Users.Administration.Validators;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(request => request.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.MiddleName)
            .MaximumLength(100)
            .When(request => !string.IsNullOrWhiteSpace(request.MiddleName));

        RuleFor(request => request.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.SecondLastName)
            .MaximumLength(100)
            .When(request => !string.IsNullOrWhiteSpace(request.SecondLastName));

        RuleFor(request => request.DocumentType)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(request => request.DocumentNumber)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(request => request.Residence)
            .NotNull();

        RuleFor(request => request.Residence.CountryId)
            .NotEmpty();

        RuleFor(request => request.Residence.DepartmentId)
            .NotEmpty();

        RuleFor(request => request.Residence.MunicipalityId)
            .NotEmpty();

        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(150);

        RuleFor(request => request.Username)
            .NotEmpty()
            .MinimumLength(4)
            .MaximumLength(100);

        RuleFor(request => request.Password)
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

        RuleFor(request => request.AssignedRoleCode)
            .MaximumLength(50)
            .When(request => !string.IsNullOrWhiteSpace(request.AssignedRoleCode));

        RuleFor(request => request.ProfessionalCard)
            .MaximumLength(80)
            .When(request => !string.IsNullOrWhiteSpace(request.ProfessionalCard));

        RuleFor(request => request.ProfessionalCardFilePath)
            .MaximumLength(255)
            .When(request => !string.IsNullOrWhiteSpace(request.ProfessionalCardFilePath));
    }
}
