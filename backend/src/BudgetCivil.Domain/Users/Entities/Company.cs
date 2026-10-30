using BudgetCivil.Domain.Catalogs;

namespace BudgetCivil.Domain.Users.Entities;

public sealed class Company
{
    public Guid Id { get; set; }

    public string BusinessName { get; set; } = string.Empty;

    public string TaxId { get; set; } = string.Empty;

    public string? CompanyType { get; set; }

    public string? TerritorialLevel { get; set; }

    public Guid? CountryId { get; set; }

    public Guid? DepartmentId { get; set; }

    public Guid? MunicipalityId { get; set; }

    public string? RepresentativeMode { get; set; }

    public Guid? RepresentativeUserId { get; set; }

    public string? RepresentativeFullName { get; set; }

    public Guid? RepresentativeDocumentTypeId { get; set; }

    public string? RepresentativeDocumentNumber { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid? CreatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Guid? LastModifiedByUserId { get; set; }

    public DateTime? LastModifiedAtUtc { get; set; }

    public ICollection<UserAccount> UserAccounts { get; set; } = new List<UserAccount>();

    public Country? Country { get; set; }

    public Department? Department { get; set; }

    public Municipality? Municipality { get; set; }
}
