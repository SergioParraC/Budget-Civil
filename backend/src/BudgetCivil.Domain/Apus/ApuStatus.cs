namespace BudgetCivil.Domain.Apus;

/// <summary>
/// Estado de completitud del APU (`REQ-APUS-03`, `D-48`). No depende del precio: la falta de precio se
/// informa solo en la vista previa de costo (`REQ-APUS-19`).
/// </summary>
public enum ApuStatus
{
    Completo = 1,
    FaltaInformacion = 2
}

public static class ApuStatusCodes
{
    public const string Completo = "COMPLETO";
    public const string FaltaInformacion = "FALTA_INFORMACION";

    public static string ToCode(ApuStatus status)
    {
        return status switch
        {
            ApuStatus.Completo => Completo,
            _ => FaltaInformacion
        };
    }

    public static ApuStatus Parse(string? code)
    {
        return string.Equals(code?.Trim(), Completo, StringComparison.OrdinalIgnoreCase)
            ? ApuStatus.Completo
            : ApuStatus.FaltaInformacion;
    }
}
