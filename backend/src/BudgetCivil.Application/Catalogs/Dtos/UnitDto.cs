namespace BudgetCivil.Application.Catalogs.Dtos;

public sealed class UnitDto
{
    public int Id { get; init; }

    public string ShortUnit { get; init; } = string.Empty;

    public string Unit { get; init; } = string.Empty;
}