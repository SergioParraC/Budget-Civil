using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Users.Search;
using BudgetCivil.Domain.Users.Entities;
using BudgetCivil.Infrastructure.Persistence.Repositories;
using BudgetCivil.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Tests.Integration;

/// <summary>
/// Búsqueda del representante legal (`D-87`): el tipo y el número de documento son obligatorios y el
/// Administrador queda fuera de los candidatos.
/// </summary>
public sealed class RepresentativeCandidateRepositoryTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task Un_director_encuentra_candidatos_por_tipo_y_numero_de_documento()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        Guid directorId;
        Guid candidateId;
        Guid inactiveCandidateId;

        await using (var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString))
        {
            var directorRole = await context.Roles.SingleAsync(role => role.Code == "DIRECTOR");
            var estimatorRole = await context.Roles.SingleAsync(role => role.Code == "PRESUPUESTADOR");
            var adminRole = await context.Roles.SingleAsync(role => role.Code == "ADMINISTRADOR");

            var director = CreateUser(directorRole.Id, "CC", $"900{suffix}", accountEnabled: true);
            var candidate = CreateUser(estimatorRole.Id, "CC", $"900{suffix}", accountEnabled: true);
            var inactiveCandidate = CreateUser(estimatorRole.Id, "CC", $"900{suffix}", accountEnabled: false);
            var administrator = CreateUser(adminRole.Id, "CC", $"900{suffix}", accountEnabled: true);

            context.UserAccounts.AddRange(director, candidate, inactiveCandidate, administrator);
            await context.SaveChangesAsync();

            directorId = director.Id;
            candidateId = candidate.Id;
            inactiveCandidateId = inactiveCandidate.Id;
        }

        await using var readContext = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var repository = new UserSearchRepository(readContext);

        var candidates = await repository.SearchRepresentativeCandidatesAsync(
            directorId,
            new UserSearchQuery { DocumentType = "CC", DocumentNumber = $"900{suffix}" });

        Assert.Contains(candidates, user => user.UserId == candidateId);
        Assert.DoesNotContain(candidates, user => user.UserId == inactiveCandidateId);
        Assert.DoesNotContain(candidates, user => user.RoleCode == "ADMINISTRADOR");
        Assert.All(candidates, user => Assert.Equal("CC", user.DocumentType));
    }

    [Fact]
    public async Task El_tipo_y_el_numero_de_documento_son_obligatorios_en_el_repositorio()
    {
        await using var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var repository = new UserSearchRepository(context);

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            repository.SearchRepresentativeCandidatesAsync(
                Guid.NewGuid(),
                new UserSearchQuery { DocumentType = "CC" }));
    }

    [Fact]
    public async Task Un_presupuestador_no_puede_buscar_representantes()
    {
        Guid estimatorId;

        await using (var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString))
        {
            var estimatorRole = await context.Roles.SingleAsync(role => role.Code == "PRESUPUESTADOR");
            var estimator = CreateUser(estimatorRole.Id, "CC", $"901{Guid.NewGuid():N}"[..11], accountEnabled: true);
            context.UserAccounts.Add(estimator);
            await context.SaveChangesAsync();
            estimatorId = estimator.Id;
        }

        await using var readContext = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var repository = new UserSearchRepository(readContext);

        await Assert.ThrowsAsync<ForbiddenAppException>(() =>
            repository.SearchRepresentativeCandidatesAsync(
                estimatorId,
                new UserSearchQuery { DocumentType = "CC", DocumentNumber = "901" }));
    }

    private static UserAccount CreateUser(Guid roleId, string documentType, string documentNumber, bool accountEnabled)
    {
        var suffix = Guid.NewGuid().ToString("N");

        return new UserAccount
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            Email = $"{suffix}@example.com",
            Username = $"user_{suffix}",
            PasswordHash = "hash",
            AccountEnabled = accountEnabled,
            FirstName = "Representante",
            LastName = suffix,
            DocumentType = documentType,
            DocumentNumber = documentNumber,
            BirthDate = new DateOnly(1990, 1, 1)
        };
    }
}
