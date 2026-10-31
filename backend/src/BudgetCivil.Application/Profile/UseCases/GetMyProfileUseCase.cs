using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Dtos;
using BudgetCivil.Application.Repositories;

namespace BudgetCivil.Application.UseCases;

public sealed class GetMyProfileUseCase(IProfileRepository repository)
{
    public async Task<GetMyProfileResponse> ExecuteAsync(Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await repository.GetUserWithProfileByIdAsync(actorUserId, cancellationToken);
        if (actor is null)
        {
            throw new ForbiddenAppException("Authenticated user does not exist.", "AUTH_USER_NOT_FOUND");
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
                FirstName = actor.FirstName,
                MiddleName = actor.MiddleName,
                LastName = actor.LastName,
                SecondLastName = actor.SecondLastName,
                DocumentType = actor.DocumentType,
                DocumentNumber = actor.DocumentNumber,
                BirthDate = actor.BirthDate,
                Residence = new ProfileResidenceResponse
                {
                    City = actor.Municipality?.Name,
                    Department = actor.Department?.Name,
                    Country = actor.Country?.Name,
                    CountryId = actor.CountryId,
                    DepartmentId = actor.DepartmentId,
                    MunicipalityId = actor.MunicipalityId
                },
                ProfessionalCard = actor.ProfessionalCard,
                ProfessionalCardIssuedAt = actor.ProfessionalCardIssuedAt,
                ProfessionalCardFilePath = actor.ProfessionalCardFilePath
            }
        };
    }
}
