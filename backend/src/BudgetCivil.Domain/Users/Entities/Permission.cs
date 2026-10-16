namespace BudgetCivil.Domain.Users.Entities;

public sealed class Permission
{
    public Guid Id { get; set; }

    public string PermissionKey { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public string SeedVersion { get; set; } = "v1";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
