namespace BudgetCivil.Application.Users.Bootstrap.Dtos;

/// <summary>
/// `REQ-IDENTIDAD-27` / `SC-IDENTIDAD-69`: el bootstrap crea **solo** al primer Administrador, que no
/// pertenece a ninguna compañía. La empresa demo, si se necesita, se crea después con `POST /companies`.
/// </summary>
public sealed class BootstrapRequest
{
    public BootstrapAdminProfileRequest AdminProfile { get; init; } = new();
}

public sealed class BootstrapAdminProfileRequest
{
    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string DocumentType { get; init; } = string.Empty;

    public string DocumentNumber { get; init; } = string.Empty;

    public DateOnly BirthDate { get; init; }

    public string Email { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    // La residencia es opcional en el bootstrap: el Administrador no opera dentro de una compañía y
    // puede completarla después en su perfil (REQ-IDENTIDAD-08).
    public Guid? CountryId { get; init; }

    public Guid? DepartmentId { get; init; }

    public Guid? MunicipalityId { get; init; }

    public bool HasResidence => CountryId.HasValue || DepartmentId.HasValue || MunicipalityId.HasValue;

    public bool HasCompleteResidence =>
        CountryId.HasValue && DepartmentId.HasValue && MunicipalityId.HasValue;
}
