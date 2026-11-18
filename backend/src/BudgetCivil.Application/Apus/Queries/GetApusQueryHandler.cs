using BudgetCivil.Application.Apus.Dtos;
using BudgetCivil.Application.Apus.Repositories;
using BudgetCivil.Application.Apus.Services;
using BudgetCivil.Application.Calculation;
using BudgetCivil.Application.Common.Exceptions;

namespace BudgetCivil.Application.Apus.Queries;

public sealed class GetApusQuery
{
    public string? Search { get; init; }

    public Guid? DepartmentId { get; init; }

    public Guid? MunicipalityId { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 10;
}

/// <summary>
/// Listado de APUs de otros proyectos de la compañía (`REQ-APUS-05`, `SC-APUS-21`…`25`, `94`, `95`,
/// `D7`). Excluye el proyecto de la ruta y no expone cantidad de obra ni AIU.
/// </summary>
public sealed class GetApusQueryHandler(
    ApuAccessService apuAccessService,
    IApuRepository apuRepository)
{
    public async Task<ApuListResponse> ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        GetApusQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Page < 1)
        {
            throw new ValidationAppException("page debe ser mayor que cero.", ApuRules.ValidationError);
        }

        if (query.PageSize is < 1 or > 100)
        {
            throw new ValidationAppException(
                "pageSize debe estar entre 1 y 100.",
                ApuRules.PageSizeInvalid);
        }

        var (_, access) = await apuAccessService.EnsureReadAsync(actorUserId, projectId, cancellationToken);

        var page = await apuRepository.ListOtherProjectsAsync(
            projectId,
            access.CompanyId,
            query.Search,
            query.DepartmentId,
            query.MunicipalityId,
            query.Page,
            query.PageSize,
            cancellationToken);

        var settings = await apuRepository.GetProjectSettingsAsync(projectId, cancellationToken)
            ?? throw new NotFoundAppException("Project was not found.", "PROJECT_NOT_FOUND");

        var costSettings = ApuDetailService.ToCostSettings(settings);

        var items = page.Items
            .Select(row => ApuMapper.ToListItem(
                row,
                ApuCostCalculator.CalculateApu(
                    ApuDetailService.ToCostInputs(row.Lines),
                    costSettings)))
            .ToList();

        return new ApuListResponse
        {
            Items = items,
            Page = page.Page,
            PageSize = page.PageSize,
            TotalItems = page.TotalItems
        };
    }
}
