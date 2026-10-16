using BudgetCivil.Application.Profile.Dtos;
using FluentValidation;

namespace BudgetCivil.Application.Profile.Validators;

public sealed class UpdateMyProfileRequestValidator : AbstractValidator<UpdateMyProfileRequest>
{
    public UpdateMyProfileRequestValidator()
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

        RuleFor(request => request.ProfessionalCard)
            .MaximumLength(80)
            .When(request => !string.IsNullOrWhiteSpace(request.ProfessionalCard));

        RuleFor(request => request.ProfessionalCardFilePath)
            .MaximumLength(255)
            .When(request => !string.IsNullOrWhiteSpace(request.ProfessionalCardFilePath));
    }
}
