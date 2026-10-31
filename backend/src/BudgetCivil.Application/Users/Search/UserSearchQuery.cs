namespace BudgetCivil.Application.Users.Search;

public sealed class UserSearchQuery
{
    public string? Email { get; init; }

    public string? Username { get; init; }

    public string? FullName { get; init; }

    public string? DocumentType { get; init; }

    public string? DocumentNumber { get; init; }

    /// <summary>
    /// Búsqueda libre: aplica OR sobre correo, usuario, nombre completo y número de documento.
    /// </summary>
    public string? Search { get; init; }

    /// <summary>Filtro por código de rol (`DIRECTOR`, `PRESUPUESTADOR`, `AUDITOR`).</summary>
    public string? RoleCode { get; init; }

    /// <summary>
    /// Solo usuarios sin vínculo vigente con ninguna compañía (disponibles para vincular).
    /// </summary>
    public bool UnassignedOnly { get; init; }

    /// <summary>
    /// Restringe la búsqueda a los usuarios con vínculo vigente en esa compañía. El Administrador puede
    /// consultar cualquier compañía; los demás roles solo la propia.
    /// </summary>
    public Guid? CompanyId { get; init; }

    public bool? AccountEnabled { get; init; }
}
