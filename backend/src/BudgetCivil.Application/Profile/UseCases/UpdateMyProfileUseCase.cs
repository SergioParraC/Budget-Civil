using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Profile.Dtos;
using BudgetCivil.Application.Profile.Repositories;
using FluentValidation;

namespace BudgetCivil.Application.Profile.UseCases;

public sealed class UpdateMyProfileUseCase(
    IProfileRepository repository,
    IValidator<UpdateMyProfileRequest> validator)
{
    public async Task<UpdateMyProfileResponse> ExecuteAsync(
        Guid actorUserId,
        UpdateMyProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var actor = await repository.GetUserWithProfileByIdAsync(actorUserId, cancellationToken);
        if (actor is null)
        {
            throw new ForbiddenAppException("Authenticated user does not exist.", "AUTH_USER_NOT_FOUND");
        }

        if (actor.Profile is null)
        {
            throw new ConflictAppException("User profile does not exist.", "PROFILE_NOT_FOUND");
        }

        if (await repository.EmailExistsAsync(request.Email, actor.Id, cancellationToken))
        {
            throw new ConflictAppException("Email already exists.", "EMAIL_ALREADY_EXISTS");
        }

        if (await repository.UsernameExistsAsync(request.Username, actor.Id, cancellationToken))
        {
            throw new ConflictAppException("Username already exists.", "USERNAME_ALREADY_EXISTS");
        }

        if (await repository.DocumentExistsAsync(request.DocumentType, request.DocumentNumber, actor.Id, cancellationToken))
        {
            throw new ConflictAppException("Document already exists.", "DOCUMENT_ALREADY_EXISTS");
        }

        var utcNow = DateTime.UtcNow;

        actor.Email = request.Email.Trim();
        actor.Username = request.Username.Trim();
        actor.UpdatedAtUtc = utcNow;

        actor.Profile.FirstName = request.FirstName.Trim();
        actor.Profile.MiddleName = NormalizeOptionalText(request.MiddleName);
        actor.Profile.LastName = request.LastName.Trim();
        actor.Profile.SecondLastName = NormalizeOptionalText(request.SecondLastName);
        actor.Profile.DocumentType = request.DocumentType.Trim();
        actor.Profile.DocumentNumber = request.DocumentNumber.Trim();
        actor.Profile.BirthDate = request.BirthDate;
        actor.Profile.ResidenceCity = request.Residence.City.Trim();
        actor.Profile.ResidenceDepartment = request.Residence.Department.Trim();
        actor.Profile.ResidenceCountry = request.Residence.Country.Trim();
        actor.Profile.ProfessionalCard = NormalizeOptionalText(request.ProfessionalCard);
        actor.Profile.ProfessionalCardIssuedAt = request.ProfessionalCardIssuedAt;
        actor.Profile.ProfessionalCardFilePath = NormalizeOptionalText(request.ProfessionalCardFilePath);

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
