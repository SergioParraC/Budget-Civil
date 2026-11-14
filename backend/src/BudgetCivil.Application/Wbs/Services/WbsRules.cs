using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Domain.Wbs;

namespace BudgetCivil.Application.Wbs.Services;

/// <summary>
/// Validaciones de entrada que el contrato traduce a códigos estables de `ProblemDetails` (`D-100`).
/// </summary>
public static class WbsRules
{
    public const string CodeInvalid = "WBS_CODE_INVALID";
    public const string CodeDuplicated = "WBS_CODE_DUPLICATED";
    public const string QuantityInvalid = "WBS_QUANTITY_INVALID";
    public const string ValidationError = "VALIDATION_ERROR";
    public const string ConcurrencyConflict = "WBS_CONCURRENCY_CONFLICT";

    /// <summary>Exige un código 1–10 del conjunto de `R-19` y lo devuelve recortado (`D-117`).</summary>
    public static string EnsureValidCode(string? code)
    {
        if (!WbsCodePolicy.IsValidCode(code))
        {
            throw new ValidationAppException(
                "El código del EDT debe tener entre 1 y 10 caracteres y usar solo A-Z, a-z, 0-9 y . - _ / * = ( ) % & # ¡ ' ? ¿.",
                CodeInvalid);
        }

        return WbsCodePolicy.NormalizeCode(code);
    }

    /// <summary>La cantidad de obra debe ser mayor que cero (`R-21`, `SC-EDT-25`…`27`).</summary>
    public static decimal EnsureValidQuantity(decimal quantityWork)
    {
        if (quantityWork <= 0m)
        {
            throw new ValidationAppException(
                "La cantidad de obra debe ser mayor que cero.",
                QuantityInvalid);
        }

        return quantityWork;
    }

    /// <summary>Nombre de capítulo no vacío y de hasta 255 caracteres.</summary>
    public static string EnsureValidName(string? name)
    {
        try
        {
            return WbsChapter.NormalizeName(name);
        }
        catch (InvalidOperationException exception)
        {
            throw new ValidationAppException(exception.Message, ValidationError);
        }
    }

    /// <summary>Convierte el `rowVersion` Base64 del contrato a bytes; vacío o inválido es entrada inválida.</summary>
    public static byte[] ParseRowVersion(string? rowVersion)
    {
        if (string.IsNullOrWhiteSpace(rowVersion))
        {
            throw new ValidationAppException("rowVersion es obligatorio.", ValidationError);
        }

        try
        {
            return Convert.FromBase64String(rowVersion.Trim());
        }
        catch (FormatException)
        {
            throw new ValidationAppException("rowVersion no es un Base64 válido.", ValidationError);
        }
    }

    /// <summary>Compara la versión vigente con la enviada y responde `409 WBS_CONCURRENCY_CONFLICT`.</summary>
    public static void EnsureCurrentRowVersion(byte[] currentRowVersion, byte[] requestedRowVersion)
    {
        if (!currentRowVersion.AsSpan().SequenceEqual(requestedRowVersion))
        {
            throw new ConflictAppException(
                "El nodo fue modificado por otro usuario; refresca el árbol y reintenta.",
                ConcurrencyConflict);
        }
    }

    public static string ToRowVersion(byte[] rowVersion) => Convert.ToBase64String(rowVersion);
}
