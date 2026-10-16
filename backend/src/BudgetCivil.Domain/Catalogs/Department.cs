namespace BudgetCivil.Domain.Catalogs;

public sealed class Department
{
    public Guid Id { get; set; }

    public Guid CountryId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}