using BudgetCivil.Application.Apus.Dtos;
using BudgetCivil.Application.Apus.Repositories;
using BudgetCivil.Application.Apus.Services;
using BudgetCivil.Application.Calculation;
using BudgetCivil.Application.Common.Exceptions;

namespace BudgetCivil.Application.Apus.Queries;

/// <summary>
/// Reporte de uso de insumos (`REQ-APUS-12`, `REQ-APUS-13`, `SC-APUS-56`…`62`, `98`, `102`): agrupa por
/// `resourceType`, discrimina cada insumo con su cantidad física, precio unitario y total, y devuelve
/// subtotales por tipo y total general. Incluye los insumos sin uso con cero y expande un solo nivel de
/// Matriz, con desperdicio y sin `yield`.
/// </summary>
public sealed class GetResourceUsageQueryHandler(
    ApuAccessService apuAccessService,
    IApuRepository apuRepository)
{
    public async Task<ResourceUsageResponse> ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        await apuAccessService.EnsureReadAsync(actorUserId, projectId, cancellationToken);

        var data = await apuRepository.GetUsageDataAsync(projectId, cancellationToken);
        var settings = await apuRepository.GetProjectSettingsAsync(projectId, cancellationToken)
            ?? throw new NotFoundAppException("Project was not found.", "PROJECT_NOT_FOUND");

        var usage = ResourceUsageCalculator.Calculate(
            data.DirectLines
                .Select(line => new UsageDirectLine(
                    line.ResourceId,
                    line.Consumption,
                    line.WastePercentage,
                    line.QuantityWork))
                .ToList(),
            data.MatrixLines
                .Select(line => new UsageMatrixLine(
                    line.MatrixResourceId,
                    line.Consumption,
                    line.WastePercentage,
                    line.QuantityWork))
                .ToList(),
            data.MatrixComponents
                .Select(component => new UsageMatrixComponent(
                    component.MatrixResourceId,
                    component.ComponentResourceId,
                    component.Consumption,
                    component.WastePercentage))
                .ToList(),
            settings.QuantityDecimals);

        var report = ResourceUsageReportService.Build(
            data.Resources,
            usage,
            settings.RoundingDecimals);

        return new ResourceUsageResponse
        {
            ProjectId = projectId,
            Resources = report.Resources
                .Select(group => new ResourceUsageGroupDto
                {
                    ResourceType = group.ResourceType,
                    ResourceTypeName = group.ResourceTypeName,
                    Subtotal = group.Subtotal,
                    Items = group.Items
                        .Select(item => new ResourceUsageItemDto
                        {
                            ResourceId = item.ResourceId,
                            Description = item.Description,
                            Unit = item.Unit,
                            UnitPrice = item.UnitPrice,
                            Quantity = item.Quantity,
                            TotalPrice = item.TotalPrice
                        })
                        .ToList()
                })
                .ToList(),
            TotalCost = report.TotalCost
        };
    }
}
