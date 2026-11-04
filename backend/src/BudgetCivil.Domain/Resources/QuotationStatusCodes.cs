namespace BudgetCivil.Domain.Resources;

/// <summary>Códigos persistidos y expuestos de <see cref="QuotationStatus"/> (`D-69`).</summary>
public static class QuotationStatusCodes
{
    public const string FaltaInformacion = "FALTA_INFORMACION";
    public const string PorCotizar = "POR_COTIZAR";
    public const string Cotizado = "COTIZADO";

    public static string ToCode(QuotationStatus status)
    {
        return status switch
        {
            QuotationStatus.FaltaInformacion => FaltaInformacion,
            QuotationStatus.PorCotizar => PorCotizar,
            QuotationStatus.Cotizado => Cotizado,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown quotation status.")
        };
    }

    public static bool TryParse(string? value, out QuotationStatus status)
    {
        switch (value?.Trim().ToUpperInvariant())
        {
            case FaltaInformacion:
                status = QuotationStatus.FaltaInformacion;
                return true;
            case PorCotizar:
                status = QuotationStatus.PorCotizar;
                return true;
            case Cotizado:
                status = QuotationStatus.Cotizado;
                return true;
            default:
                status = default;
                return false;
        }
    }

    public static QuotationStatus Parse(string value)
    {
        return TryParse(value, out var status)
            ? status
            : throw new InvalidOperationException($"Unknown quotation status '{value}'.");
    }
}
