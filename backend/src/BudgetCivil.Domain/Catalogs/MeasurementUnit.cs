namespace BudgetCivil.Domain.Catalogs;

/// <summary>
/// Catálogo global de unidades de medida (`REQ-UNIDADES-01`, `D-16`). Es una lista de sugerencias para APU
/// e insumos: no tiene clave foránea desde ellos y no se llena automáticamente al registrar una unidad.
/// </summary>
public sealed class MeasurementUnit
{
    public const int ShortUnitMaxLength = 10;
    public const int UnitMaxLength = 100;

    private MeasurementUnit()
    {
    }

    public int Id { get; private set; }

    public string ShortUnit { get; private set; } = string.Empty;

    public string Unit { get; private set; } = string.Empty;

    public static MeasurementUnit Create(string? shortUnit, string? unit)
    {
        return new MeasurementUnit
        {
            ShortUnit = NormalizeShortUnit(shortUnit),
            Unit = NormalizeUnit(unit)
        };
    }

    /// <summary>La sigla tiene entre 1 y 10 caracteres (`REQ-UNIDADES-01`, `SC-UNIDADES-03`).</summary>
    public static string NormalizeShortUnit(string? shortUnit)
    {
        var normalized = shortUnit?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > ShortUnitMaxLength)
        {
            throw new InvalidOperationException("Measurement unit abbreviation is invalid.");
        }

        return normalized;
    }

    /// <summary>El nombre tiene entre 1 y 100 caracteres (`REQ-UNIDADES-01`, `SC-UNIDADES-03`).</summary>
    public static string NormalizeUnit(string? unit)
    {
        var normalized = unit?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > UnitMaxLength)
        {
            throw new InvalidOperationException("Measurement unit name is invalid.");
        }

        return normalized;
    }
}