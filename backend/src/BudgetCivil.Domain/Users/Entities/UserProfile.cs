using BudgetCivil.Domain.Catalogs;

namespace BudgetCivil.Domain.Users.Entities;

public sealed class UserProfile
{
    public Guid UserId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string? MiddleName { get; set; }

    public string LastName { get; set; } = string.Empty;

    public string? SecondLastName { get; set; }

    public string DocumentType { get; set; } = string.Empty;

    public string DocumentNumber { get; set; } = string.Empty;

    public DateOnly BirthDate { get; set; }

    public Guid? MunicipalityId { get; set; }

    public Guid? DepartmentId { get; set; }

    public Guid? CountryId { get; set; }

    public string? ProfessionalCard { get; set; }

    public DateOnly? ProfessionalCardIssuedAt { get; set; }

    public string? ProfessionalCardFilePath { get; set; }

    public UserAccount UserAccount { get; set; } = null!;

    public Municipality? Municipality { get; set; }

    public Department? Department { get; set; }

    public Country? Country { get; set; }
}
