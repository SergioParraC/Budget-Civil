namespace BudgetCivil.Domain.Wbs;

/// <summary>
/// Reglas puras del código manual del EDT (`D-115`, `D-117`, `R-19`). El código completo lo escribe el
/// usuario al crear o editar; no hay composición con el padre, numeración automática ni respaldo.
/// </summary>
public static class WbsCodePolicy
{
    public const int MinLength = 1;
    public const int MaxLength = 10;

    // R-19: A-Z, a-z, 0-9 y los símbolos . - _ / * = ( ) % & # ¡ ' ? ¿
    private const string AllowedSymbols = ".-_/*=()%&#¡'?¿";

    /// <summary>Valida el código ya recortado: 1–10 caracteres del conjunto cerrado de `R-19`.</summary>
    public static bool IsValidCode(string? code)
    {
        if (code is null)
        {
            return false;
        }

        var normalized = code.Trim();
        if (normalized.Length is < MinLength or > MaxLength)
        {
            return false;
        }

        foreach (var character in normalized)
        {
            if (IsAsciiLetterOrDigit(character) || AllowedSymbols.Contains(character))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    /// <summary>
    /// Recorta el valor y conserva las mayúsculas y minúsculas escritas por el usuario. Un código vacío o
    /// fuera de `R-19` es un error de dominio (`WBS_CODE_INVALID`).
    /// </summary>
    public static string NormalizeCode(string? code)
    {
        var normalized = code?.Trim() ?? string.Empty;
        if (!IsValidCode(normalized))
        {
            throw new InvalidOperationException("WBS code is invalid.");
        }

        return normalized;
    }

    public static bool IsAsciiLetterOrDigit(char character) =>
        character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9';
}
