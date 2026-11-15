namespace BudgetCivil.Application.Wbs.Repositories;

public sealed record ApuSummary(
    Guid ApuId,
    string Status,
    string Description,
    string Unit,
    string RowVersion);

/// <summary>
/// Referencia del APU por ítem del árbol (`REQ-EDT-20`, `D7`). La implementa `Infrastructure` con una
/// sola consulta por proyecto y el módulo `M-03` no conoce `M-05`.
/// </summary>
public interface IApuSummaryReader
{
    Task<IReadOnlyDictionary<Guid, ApuSummary>> GetByProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);
}
