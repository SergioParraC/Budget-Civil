namespace BudgetCivil.Domain.Resources;

/// <summary>Catálogo de tipos de insumo (`D-16`, `PLAN §3.3.7`).</summary>
public sealed class ResourceType
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
