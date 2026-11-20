using BudgetCivil.Application.Apus.Dtos;
using BudgetCivil.Application.Apus.Repositories;
using BudgetCivil.Application.Calculation;
using BudgetCivil.Domain.Apus;

namespace BudgetCivil.Application.Apus.Services;

/// <summary>
/// Carga la composición base del APU para la edición. Los importes de costo se calculan en el frontend;
/// la cantidad de obra solo se informa para APUs propios.
/// </summary>
public sealed class ApuDetailService(
    IApuRepository apuRepository,
    IWbsItemReader wbsItemReader)
{
    public async Task<ApuDetailResponse> BuildAsync(
        Guid routeProjectId,
        Apu apu,
        CancellationToken cancellationToken = default)
    {
        var payload = await apuRepository.GetCostPayloadAsync(apu.ProjectId, apu.Id, cancellationToken)
            ?? new ApuCostPayload(apu.Id, apu.ProjectId, []);

        var item = await wbsItemReader.GetItemByApuAsync(apu.Id, cancellationToken);
        var status = RecalculateStatus(apu, payload.Lines);

        return ApuMapper.ToDetail(
            apu,
            payload,
            status,
            item,
            includeQuantityWork: apu.ProjectId == routeProjectId);
    }

    public static IReadOnlyList<ApuCostLineInput> ToCostInputs(IReadOnlyList<ApuCostLineSource> lines)
    {
        return lines
            .Select(line => new ApuCostLineInput(
                line.LineId,
                line.ResourceId,
                line.ResourceTypeCode,
                line.Description,
                line.Unit,
                line.UnitPrice,
                line.Consumption,
                line.WastePercentage,
                line.MatrixComponents
                    .Select(component => new MatrixComponentInput(
                        component.ComponentResourceId,
                        component.ResourceTypeCode,
                        component.Description,
                        component.Unit,
                        component.UnitPrice,
                        component.Consumption,
                        component.WastePercentage))
                    .ToList()))
            .ToList();
    }

    public static CostSettings ToCostSettings(ProjectCostSettings settings)
    {
        return new CostSettings(
            settings.RoundingDecimals,
            settings.QuantityDecimals,
            settings.AiuAdminPercent,
            settings.AiuUnforeseenPercent,
            settings.AiuProfitPercent);
    }

    /// <summary>
    /// Recalcula la completitud del APU con la completitud de las Matrices que referencia (`INC-06`).
    /// </summary>
    public static ApuStatus RecalculateStatus(Apu apu, IReadOnlyList<ApuCostLineSource> lines)
    {
        var matricesComplete = lines
            .Where(line => line.IsMatrix)
            .All(line => line.MatrixComplete);

        return apu.CalculateStatus(matricesComplete);
    }
}
