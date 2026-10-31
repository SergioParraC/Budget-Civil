using System.ComponentModel.DataAnnotations.Schema;
using BudgetCivil.Domain.Catalogs;
using BudgetCivil.Domain.Memberships;

namespace BudgetCivil.Domain.Users.Entities;

public sealed class UserAccount
{
    public Guid Id { get; set; }

    public Guid RoleId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool AccountEnabled { get; set; } = true;

    public bool AccountLocked { get; set; }

    public int FailedLoginAttempts { get; set; }

    public DateTime? LastLoginAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public string FirstName { get; set; } = string.Empty;

    public string? MiddleName { get; set; }

    public string LastName { get; set; } = string.Empty;

    public string? SecondLastName { get; set; }

    public string DocumentType { get; set; } = string.Empty;

    public string DocumentNumber { get; set; } = string.Empty;

    public DateOnly BirthDate { get; set; }

    public Guid? CountryId { get; set; }

    public Guid? DepartmentId { get; set; }

    public Guid? MunicipalityId { get; set; }

    public string? ProfessionalCard { get; set; }

    public DateOnly? ProfessionalCardIssuedAt { get; set; }

    public string? ProfessionalCardFilePath { get; set; }

    public Role Role { get; set; } = null!;

    public Country? Country { get; set; }

    public Department? Department { get; set; }

    public Municipality? Municipality { get; set; }

    /// <summary>
    /// Historial de vínculos con compañías (tabla <c>users_company</c>). Se autocarga en las consultas
    /// porque <see cref="CompanyId"/> se deriva de la membresía vigente.
    /// </summary>
    public ICollection<CompanyMembership> Memberships { get; set; } = new List<CompanyMembership>();

    /// <summary>Membresía vigente del usuario, si tiene una.</summary>
    [NotMapped]
    public CompanyMembership? ActiveMembership =>
        Memberships
            .Where(membership => membership.AccountEnabled)
            .OrderByDescending(membership => membership.UpdatedAtUtc)
            .FirstOrDefault();

    /// <summary>
    /// Compañía vigente del usuario. No es una columna: se deriva de <c>users_company</c>
    /// (<c>account_enable = 1</c>), que es la única fuente de verdad de la pertenencia.
    /// </summary>
    [NotMapped]
    public Guid? CompanyId => ActiveMembership?.CompanyId;

    public ICollection<PasswordRecoveryGrant> RecoveryGrants { get; set; } = new List<PasswordRecoveryGrant>();

    public ICollection<PasswordRecoveryGrant> EnabledRecoveryGrants { get; set; } = new List<PasswordRecoveryGrant>();

    public ICollection<SecurityAuditEvent> SecurityAuditEvents { get; set; } = new List<SecurityAuditEvent>();
}