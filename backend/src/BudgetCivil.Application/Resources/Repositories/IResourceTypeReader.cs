namespace BudgetCivil.Application.Resources.Repositories;

public sealed record ResourceTypeOption(Guid Id, string Code, string Name);

/// <summary>Lectura de los tipos de insumo activos para los formularios de APU e insumo (`REQ-INSUMOS-16`).</summary>
public interface IResourceTypeReader
{
    Task<IReadOnlyList<ResourceTypeOption>> ListActiveAsync(
        CancellationToken cancellationToken = default);
}