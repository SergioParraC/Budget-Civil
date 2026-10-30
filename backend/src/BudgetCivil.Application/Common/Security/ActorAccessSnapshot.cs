namespace BudgetCivil.Application.Common.Security;

/// <summary>
/// Foto de autorización y alcance del actor que necesitan el middleware de compañía y el manejador de
/// permisos. Vive en `Application` para que `Api` no consulte la persistencia (`§3.4.6`).
/// </summary>
public sealed record ActorAccessSnapshot(
    Guid UserId,
    Guid? CompanyId,
    Guid RoleId,
    string RoleCode,
    bool AccountEnabled,
    bool AccountLocked,
    bool RoleIsActive,
    bool? CompanyIsActive,
    IReadOnlyCollection<string> GrantedPermissionKeys);
