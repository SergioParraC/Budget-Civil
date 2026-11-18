using BudgetCivil.Application.Wbs.Dtos;
using BudgetCivil.Application.Wbs.Repositories;
using BudgetCivil.Application.Wbs.Services;

namespace BudgetCivil.Application.Wbs.Queries;

/// <summary>
/// Última actividad y participantes del EDT (`REQ-EDT-19`, `SC-EDT-73`, `SC-EDT-74`, `D-120`). El
/// Auditor no accede (`SC-EDT-76`).
/// </summary>
public sealed class GetWbsActivityQueryHandler(
    WbsAccessService wbsAccessService,
    IWbsRepository wbsRepository)
{
    public async Task<GetWbsActivityResponse> ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        await wbsAccessService.EnsureActivityReadAsync(actorUserId, projectId, cancellationToken);

        var snapshot = await wbsRepository.GetActivityAsync(projectId, cancellationToken);
        return WbsMapper.ToActivityResponse(projectId, snapshot);
    }
}
