using BudgetCivil.Domain.Catalogs;
using BudgetCivil.Domain.Projects;
using BudgetCivil.Domain.Users.Entities;
using BudgetCivil.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Tests.Integration.Infrastructure;

public sealed record CompanyTestData(
    Company Company,
    UserAccount Director,
    Country Country,
    Department Department,
    Municipality Municipality,
    BaseCurrency BaseCurrency);

public static class TestDataFactory
{
    public static async Task<CompanyTestData> CreateCompanyWithDirectorAsync(BudgetCivilDbContext context)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        var country = new Country { Id = Guid.NewGuid(), Code = $"C{suffix}", Name = $"Pais {suffix}" };
        var department = new Department
        {
            Id = Guid.NewGuid(),
            CountryId = country.Id,
            Code = $"D{suffix}",
            Name = $"Departamento {suffix}"
        };
        var municipality = new Municipality
        {
            Id = Guid.NewGuid(),
            DepartmentId = department.Id,
            Code = $"M{suffix}",
            Name = $"Municipio {suffix}"
        };
        var baseCurrency = new BaseCurrency
        {
            Id = Guid.NewGuid(),
            IsoCode = $"X{suffix[..3]}",
            Name = $"Moneda {suffix}"
        };

        context.AddRange(country, department, municipality, baseCurrency);

        var directorRole = await context.Roles.SingleAsync(role => role.Code == "DIRECTOR");
        var companyId = Guid.NewGuid();

        var company = new Company
        {
            Id = companyId,
            BusinessName = $"Empresa {suffix}",
            TaxId = $"NIT-{suffix}",
            CompanyType = "PRIVADA",
            CountryId = country.Id,
            DepartmentId = department.Id,
            MunicipalityId = municipality.Id,
            IsActive = true
        };

        var director = new UserAccount
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            RoleId = directorRole.Id,
            Email = $"director.{suffix}@example.com",
            Username = $"director_{suffix}".ToLowerInvariant(),
            PasswordHash = "hash",
            AccountEnabled = true,
            FirstName = "Director",
            LastName = "Prueba",
            DocumentType = "CC",
            DocumentNumber = $"DOC-{suffix}",
            BirthDate = new DateOnly(1990, 1, 1),
            CountryId = country.Id,
            DepartmentId = department.Id,
            MunicipalityId = municipality.Id
        };

        context.AddRange(company, director);
        await context.SaveChangesAsync();

        return new CompanyTestData(company, director, country, department, municipality, baseCurrency);
    }

    public static Project CreateProject(CompanyTestData data, string name = "Proyecto de prueba")
    {
        var projectId = Guid.NewGuid();

        return new Project
        {
            Id = projectId,
            CompanyId = data.Company.Id,
            Name = name,
            CountryId = data.Country.Id,
            DepartmentId = data.Department.Id,
            MunicipalityId = data.Municipality.Id,
            DirectorOwnerUserId = data.Director.Id,
            CreatedByUserId = data.Director.Id,
            Status = ProjectStatus.PorCotizar,
            IsPublic = false,
            Configuration = new ProjectConfiguration
            {
                ProjectId = projectId,
                BaseCurrencyId = data.BaseCurrency.Id,
                BudgetLimit = 1000m,
                SignatureMode = Project.DefaultSignatureMode
            }
        };
    }
}
