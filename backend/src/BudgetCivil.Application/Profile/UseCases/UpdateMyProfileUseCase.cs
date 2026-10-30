using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Catalogs;
using BudgetCivil.Application.Catalogs.Repositories;
using BudgetCivil.Application.Dtos;
using BudgetCivil.Application.Repositories;
using FluentValidation;

namespace BudgetCivil.Application.UseCases;

public sealed class UpdateMyProfileUseCase(
    IProfileRepository repository,
    IGeographicCatalogRepository geographicCatalogRepository,
    IValidator<UpdateMyProfileRequest> validator)
{
    public async Task<UpdateMyProfileResponse> ExecuteAsync(
        Guid actorUserId,
        UpdateMyProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);
        await GeographicLocationRules.EnsureValidActiveLocationAsync(
            geographicCatalogRepository,
            request.Residence.CountryId,
            request.Residence.DepartmentId,
            request.Residence.MunicipalityId,
            cancellationToken);

        var actor = await repository.GetUserWithProfileByIdAsync(actorUserId, cancellationToken);
        if (actor is null)
        {
            throw new ForbiddenAppException("Authenticated user does not exist.", "AUTH_USER_NOT_FOUND");
        }

        if (await repository.EmailExistsAsync(request.Email, actor.Id, cancellationToken))
        {
            throw new ConflictAppException("Email already exists.", "EMAIL_ALREADY_EXISTS");
        }

        if (await repository.UsernameExistsAsync(request.Username, actor.Id, cancellationToken))
        {
            throw new ConflictAppException("Username already exists.", "USERNAME_ALREADY_EXISTS");
        }

        var utcNow = DateTime.UtcNow;

        actor.Email = request.Email.Trim();
        actor.Username = request.Username.Trim();
        actor.UpdatedAtUtc = utcNow;

        actor.FirstName = request.FirstName.Trim();
        actor.MiddleName = NormalizeOptionalText(request.MiddleName);
        actor.LastName = request.LastName.Trim();
        actor.SecondLastName = NormalizeOptionalText(request.SecondLastName);
        actor.DocumentType = request.DocumentType.Trim();
        actor.DocumentNumber = request.DocumentNumber.Trim();
        actor.BirthDate = request.BirthDate;
        actor.CountryId = request.Residence.CountryId;
        actor.DepartmentId = request.Residence.DepartmentId;
        actor.MunicipalityId = request.Residence.MunicipalityId;
        actor.ProfessionalCard = NormalizeOptionalText(request.ProfessionalCard);
        actor.ProfessionalCardIssuedAt = request.ProfessionalCardIssuedAt;
        actor.ProfessionalCardFilePath = NormalizeOptionalText(request.ProfessionalCardFilePath);

        await repository.SaveChangesAsync(cancellationToken);

        return new UpdateMyProfileResponse
        {
            UserId = actor.Id,
            UpdatedAtUtc = utcNow
        };
    }

    private async Task ValidateAsync(UpdateMyProfileRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (validationResult.IsValid)
        {
            return;
        }

        var message = string.Join(" | ", validationResult.Errors.Select(error => error.ErrorMessage));
        throw new ValidationAppException(message, "VALIDATION_ERROR");
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
