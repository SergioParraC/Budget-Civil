using BudgetCivil.Application.Apus.Dtos;
using BudgetCivil.Application.Apus.Repositories;
using BudgetCivil.Application.Apus.Services;
using BudgetCivil.Application.Common.Exceptions;

namespace BudgetCivil.Application.Apus.Queries;

/// <summary>
/// Detalle del APU con composición y vista previa de costo (`REQ-APUS-19`, `SC-APUS-86`…`90`). Un APU de
/// otro proyecto de la compañía también se puede consultar; uno de otra compañía responde
/// `APU_NOT_FOUND` (`SC-APUS-81`).
/// </summary>
public sealed class GetApuByIdQueryHandler(
    ApuAccessService apuAccessService,
    IApuRepository apuRepository,
    ApuDetailService apuDetailService)
{
    public async Task<ApuDetailResponse> ExecuteAsync(
        Guid actorUserId,
        Guid routeProjectId,
        Guid apuId,
        CancellationToken cancellationToken = default)
    {
        var (actor, routeAccess) = await apuAccessService.EnsureReadAsync(
            actorUserId,
            routeProjectId,
            cancellationToken);

        var apu = await apuRepository.GetAsync(routeProjectId, apuId, cancellationToken);

        if (apu is null)
        {
            apu = await apuRepository.GetByIdAsync(apuId, cancellationToken)
                ?? throw new NotFoundAppException("El APU no existe.", ApuRules.ApuNotFound);

            var sourceAccess = await apuAccessService.GetProjectAccessForActorAsync(
                actorUserId,
                apu.ProjectId,
                cancellationToken);

            if (sourceAccess is null || sourceAccess.CompanyId != routeAccess.CompanyId)
            {
                throw new NotFoundAppException("El APU no existe.", ApuRules.ApuNotFound);
            }

            ApuAccessService.EnsureCanRead(actor, sourceAccess);
        }

        return await apuDetailService.BuildAsync(routeProjectId, apu, cancellationToken);
    }
}
