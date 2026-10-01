namespace BudgetCivil.Domain.Companies;

public sealed class Company
{
    public static string NormalizeBusinessName(string businessName)
    {
        var normalized = businessName?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Business name is required.", nameof(businessName));
        }

        return normalized;
    }

    public static string NormalizeTaxId(string taxId)
    {
        var normalized = taxId?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Tax id is required.", nameof(taxId));
        }

        return normalized;
    }

    public static void ValidateTerritorialLevel(string? companyType, string? territorialLevel)
    {
        if (!string.Equals(companyType?.Trim(), "PUBLICA", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(territorialLevel))
        {
            throw new ArgumentException("Territorial level is required for public companies.", nameof(territorialLevel));
        }
    }

    public static void ValidateRepresentative(LegalRepresentativeInput? representative)
    {
        if (representative is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(representative.Mode))
        {
            throw new ArgumentException("Representative mode is required when representative data is provided.", nameof(representative.Mode));
        }

        var normalizedMode = representative.Mode.Trim().ToUpperInvariant();
        if (normalizedMode is not "USER_REFERENCE" and not "MANUAL_DATA")
        {
            throw new ArgumentException("Representative mode must be USER_REFERENCE or MANUAL_DATA.", nameof(representative.Mode));
        }

        if (normalizedMode == "USER_REFERENCE")
        {
            if (!representative.UserId.HasValue || representative.UserId.Value == Guid.Empty)
            {
                throw new ArgumentException("Representative user id is required for USER_REFERENCE mode.", nameof(representative.UserId));
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(representative.FullName))
        {
            throw new ArgumentException("Representative full name is required for MANUAL_DATA mode.", nameof(representative.FullName));
        }

        if (string.IsNullOrWhiteSpace(representative.DocumentNumber))
        {
            throw new ArgumentException("Representative document number is required for MANUAL_DATA mode.", nameof(representative.DocumentNumber));
        }
    }
}

public sealed class LegalRepresentativeInput
{
    public string? Mode { get; init; }

    public Guid? UserId { get; init; }

    public string? FullName { get; init; }

    public string? DocumentNumber { get; init; }
}
