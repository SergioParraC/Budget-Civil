using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Domain.Apus;

namespace BudgetCivil.Application.Apus.Services;

/// <summary>
/// Códigos estables de `ProblemDetails` y validaciones de entrada de `M-05` (`D10`, `D-100`).
/// </summary>
public static class ApuRules
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string ApuNotFound = "APU_NOT_FOUND";
    public const string ApuReadForbidden = "APU_READ_FORBIDDEN";
    public const string ApuWriteForbidden = "APU_WRITE_FORBIDDEN";
    public const string ApuResourceNotFound = "APU_RESOURCE_NOT_FOUND";
    public const string ApuResourceProjectMismatch = "APU_RESOURCE_PROJECT_MISMATCH";
    public const string ApuConcurrencyConflict = "APU_CONCURRENCY_CONFLICT";
    public const string ApuCompositionResolutionRequired = "APU_COMPOSITION_RESOLUTION_REQUIRED";
    public const string MatrixNotFound = "MATRIX_NOT_FOUND";
    public const string MatrixTypeInvalid = "MATRIX_TYPE_INVALID";
    public const string MatrixNestingForbidden = "MATRIX_NESTING_FORBIDDEN";
    public const string MatrixConcurrencyConflict = "MATRIX_CONCURRENCY_CONFLICT";
    public const string ResourceMatrixCopyRequiresMatrixOperation = "RESOURCE_MATRIX_COPY_REQUIRES_MATRIX_OPERATION";
    public const string PageSizeInvalid = "PAGE_SIZE_INVALID";

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

    /// <summary>Compara la versión vigente y responde `409 APU_CONCURRENCY_CONFLICT`.</summary>
    public static void EnsureApuRowVersion(byte[] current, byte[] requested)
    {
        if (!current.AsSpan().SequenceEqual(requested))
        {
            throw new ConflictAppException(
                "El APU fue modificado por otro usuario; refresca y reintenta.",
                ApuConcurrencyConflict);
        }
    }

    /// <summary>Compara la versión del insumo Matriz y responde `409 MATRIX_CONCURRENCY_CONFLICT`.</summary>
    public static void EnsureMatrixRowVersion(byte[] current, byte[] requested)
    {
        if (!current.AsSpan().SequenceEqual(requested))
        {
            throw new ConflictAppException(
                "La Matriz fue modificada por otro usuario; refresca y reintenta.",
                MatrixConcurrencyConflict);
        }
    }

    public static string ToRowVersion(byte[] rowVersion) => Convert.ToBase64String(rowVersion);

    public static decimal EnsureConsumption(decimal consumption)
    {
        try
        {
            return ApuResource.NormalizeConsumption(consumption);
        }
        catch (InvalidOperationException exception)
        {
            throw new ValidationAppException(exception.Message, ValidationError);
        }
    }

    public static decimal EnsureWastePercentage(decimal wastePercentage)
    {
        try
        {
            return ApuResource.NormalizeWastePercentage(wastePercentage);
        }
        catch (InvalidOperationException exception)
        {
            throw new ValidationAppException(exception.Message, ValidationError);
        }
    }
}
