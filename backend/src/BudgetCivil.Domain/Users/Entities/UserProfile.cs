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

    public string ResidenceCity { get; set; } = string.Empty;

    public string ResidenceDepartment { get; set; } = string.Empty;

    public string ResidenceCountry { get; set; } = string.Empty;

    public string? ProfessionalCard { get; set; }

    public DateOnly? ProfessionalCardIssuedAt { get; set; }

    public string? ProfessionalCardFilePath { get; set; }

    public UserAccount UserAccount { get; set; } = null!;
}
