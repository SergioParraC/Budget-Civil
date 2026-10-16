using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Profile.Dtos;
using BudgetCivil.Application.Profile.Repositories;

namespace BudgetCivil.Application.Profile.UseCases;

public sealed class GetMyProfileUseCase(IProfileRepository repository)
{
    public async Task<GetMyProfileResponse> ExecuteAsync(Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await repository.GetUserWithProfileByIdAsync(actorUserId, cancellationToken);
        if (actor is null)
        {
            throw new ForbiddenAppException("Authenticated user does not exist.", "AUTH_USER_NOT_FOUND");
        }

        if (actor.Profile is null)
        {
            throw new ConflictAppException("User profile does not exist.", "PROFILE_NOT_FOUND");
        }

        return new GetMyProfileResponse
        {
            UserId = actor.Id,
            Email = actor.Email,
            Username = actor.Username,
            CompanyId = actor.CompanyId,
            RoleCode = actor.Role.Code,
            AccountEnabled = actor.AccountEnabled,
            Profile = new ProfileSummaryResponse
            {
                FirstName = actor.Profile.FirstName,
                MiddleName = actor.Profile.MiddleName,
                LastName = actor.Profile.LastName,
                SecondLastName = actor.Profile.SecondLastName,
                DocumentType = actor.Profile.DocumentType,
                DocumentNumber = actor.Profile.DocumentNumber,
                BirthDate = actor.Profile.BirthDate,
                Residence = new ProfileResidenceResponse
                {
                    City = actor.Profile.Municipality?.Name,
                    Department = actor.Profile.Department?.Name,
                    Country = actor.Profile.Country?.Name,
                    CountryId = actor.Profile.CountryId,
                    DepartmentId = actor.Profile.DepartmentId,
                    MunicipalityId = actor.Profile.MunicipalityId
                },
                ProfessionalCard = actor.Profile.ProfessionalCard,
                ProfessionalCardIssuedAt = actor.Profile.ProfessionalCardIssuedAt,
                ProfessionalCardFilePath = actor.Profile.ProfessionalCardFilePath
            }
        };
    }
}
