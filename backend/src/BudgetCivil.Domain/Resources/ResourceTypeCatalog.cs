namespace BudgetCivil.Domain.Resources;

public sealed record ResourceTypeDefinition(Guid Id, string Code, string Name);

/// <summary>
/// Los siete tipos base del catálogo. Los identificadores son fijos para que el `seed` sea idempotente
/// (`D-82`, `D-107`).
/// </summary>
public static class ResourceTypeCatalog
{
    public const string Material = "MATERIAL";
    public const string Labor = "LABOR";
    public const string Tool = "TOOL";
    public const string Equipment = "EQUIPMENT";
    public const string Auxiliary = "AUXILIARY";
    public const string Work = "WORK";
    public const string Matrix = "MATRIX";

    public static readonly IReadOnlyList<ResourceTypeDefinition> Definitions =
    [
        new(Guid.Parse("11111111-1111-1111-1111-111111111101"), Material, "Materiales"),
        new(Guid.Parse("11111111-1111-1111-1111-111111111102"), Labor, "Mano de obra"),
        new(Guid.Parse("11111111-1111-1111-1111-111111111103"), Tool, "Herramienta"),
        new(Guid.Parse("11111111-1111-1111-1111-111111111104"), Equipment, "Equipos"),
        new(Guid.Parse("11111111-1111-1111-1111-111111111105"), Auxiliary, "Auxiliares"),
        new(Guid.Parse("11111111-1111-1111-1111-111111111106"), Work, "Trabajos"),
        new(Guid.Parse("11111111-1111-1111-1111-111111111107"), Matrix, "Matriz")
    ];

    public static bool IsKnownCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        return Definitions.Any(definition =>
            string.Equals(definition.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
