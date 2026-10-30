using BudgetCivil.Application.Users.Auth.Dtos;
using FluentValidation;

namespace BudgetCivil.Application.Users.Auth.Validators;

public sealed class RegisterUserRequestValidator : AbstractValidator<RegisterUserRequest>
{
    // Claves normalizadas (minúsculas, sin separadores) del payload que el cliente no puede fijar.
    private static readonly HashSet<string> ForbiddenFields =
    [
        "rolecode",
        "companyid",
        "projectid",
        "rolasignado"
    ];

    public RegisterUserRequestValidator()
    {
        RuleFor(request => request.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.MiddleName)
            .MaximumLength(100);

        RuleFor(request => request.SecondLastName)
            .MaximumLength(100);

        RuleFor(request => request.BirthDate)
            .NotEmpty()
            .WithMessage("BirthDate is required.");

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
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must include at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must include at least one number.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must include at least one special character.");

        RuleFor(request => request.DocumentType)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(request => request.DocumentNumber)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(request => request.CountryId)
            .NotEmpty();

        RuleFor(request => request.DepartmentId)
            .NotEmpty();

        RuleFor(request => request.MunicipalityId)
            .NotEmpty();

        RuleFor(request => request.ExtensionData)
            .Must(HasNoForbiddenFields)
            .WithMessage("Self-registration payload cannot include rol_asignado, company_id, or project_id.");
    }

    private static bool HasNoForbiddenFields(IDictionary<string, System.Text.Json.JsonElement>? extensionData)
    {
        if (extensionData is null || extensionData.Count == 0)
        {
            return true;
        }

        return extensionData.Keys
            .Select(NormalizeFieldName)
            .All(normalizedKey => !ForbiddenFields.Contains(normalizedKey));
    }

    private static string NormalizeFieldName(string key)
    {
        return key
            .Trim()
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
    }
}
