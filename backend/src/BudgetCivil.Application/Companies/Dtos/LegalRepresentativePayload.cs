using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Companies.Dtos;

public sealed class LegalRepresentativePayload
{
    public string? Mode { get; init; }

    public Guid? UserId { get; init; }

    public string? FullName { get; init; }

    public string? DocumentNumber { get; init; }

    public Guid? DocumentTypeId { get; init; }
}
