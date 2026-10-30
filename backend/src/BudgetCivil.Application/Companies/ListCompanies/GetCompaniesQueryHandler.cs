using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Companies.Repositories;
using BudgetCivil.Application.Companies.UseCases;
using BudgetCivil.Application.Listings.Queries;

namespace BudgetCivil.Application.Companies.ListCompanies;

public sealed class GetCompaniesQueryHandler(
    ICompanyRepository companyRepository,
    ListingQueryPipeline listingQueryPipeline)
{
    public async Task<GetCompaniesResponse> ExecuteAsync(
        Guid actorUserId,
        GetCompaniesQuery query,
        CancellationToken cancellationToken = default)
    {
        var actor = await companyRepository.GetActorByIdAsync(actorUserId, cancellationToken);
        CompanyAdministrationAuthorizationRules.EnsureActorCanReadCompanies(actor);

        var pagination = await listingQueryPipeline.ValidateAndNormalizeAsync(
            query.Page,
            query.PageSize,
            cancellationToken);

        var totalItems = await companyRepository.CountCompaniesAsync(query.Search, query.IsActive, cancellationToken);
        var items = await companyRepository.GetCompaniesAsync(
            query.Search,
            query.IsActive,
            pagination.Page,
            pagination.PageSize,
            cancellationToken);

        return new GetCompaniesResponse
        {
            Items = items
                .Select(company => new CompanyListItem
                {
                    CompanyId = company.Id,
                    BusinessName = company.BusinessName,
                    TaxId = company.TaxId,
                    CompanyType = company.CompanyType,
                    TerritorialLevel = company.TerritorialLevel,
                    CountryId = company.CountryId,
                    DepartmentId = company.DepartmentId,
                    MunicipalityId = company.MunicipalityId,
                    RepresentativeMode = company.RepresentativeMode,
                    RepresentativeUserId = company.RepresentativeUserId,
                    RepresentativeFullName = company.RepresentativeFullName,
                    RepresentativeDocumentTypeId = company.RepresentativeDocumentTypeId,
                    RepresentativeDocumentNumber = company.RepresentativeDocumentNumber,
                    IsActive = company.IsActive,
                    CreatedByUserId = company.CreatedByUserId,
                    CreatedAtUtc = company.CreatedAtUtc,
                    LastModifiedByUserId = company.LastModifiedByUserId,
                    LastModifiedAtUtc = company.LastModifiedAtUtc
                })
                .ToArray(),
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalItems = totalItems
        };
    }
}
