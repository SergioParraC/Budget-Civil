using BudgetCivil.Domain.Memberships;
using BudgetCivil.Domain.Users.Entities;
using BudgetCivil.Infrastructure.Persistence.Repositories;
using BudgetCivil.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Tests.Integration;

/// <summary>T-I-29: `User.company_id` y `ProjectMember` sin tablas de membresía (`D-98`).</summary>
public sealed class MembershipPersistenceTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task Dos_asignaciones_simultaneas_de_compania_dejan_una_sola_efectiva()
    {
        Guid targetUserId;
        Guid companyId;

        await using (var setupContext = SqlServerTestDatabase.CreateContext(fixture.ConnectionString))
        {
            var data = await TestDataFactory.CreateCompanyWithDirectorAsync(setupContext);
            companyId = data.Company.Id;

            var estimatorRole = await setupContext.Roles.SingleAsync(role => role.Code == "PRESUPUESTADOR");
            var target = new UserAccount
            {
                Id = Guid.NewGuid(),
                CompanyId = null,
                RoleId = estimatorRole.Id,
                Email = $"target.{Guid.NewGuid():N}@example.com",
                Username = $"target_{Guid.NewGuid():N}",
                PasswordHash = "hash",
                AccountEnabled = true,
                FirstName = "Destino",
                LastName = "Prueba",
                DocumentType = "CC",
                DocumentNumber = $"DOC-{Guid.NewGuid():N}",
                BirthDate = new DateOnly(1995, 6, 7)
            };

            setupContext.UserAccounts.Add(target);
            await setupContext.SaveChangesAsync();
            targetUserId = target.Id;
        }

        await using var firstContext = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        await using var secondContext = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);

        var firstRepository = new CompanyMembershipRepository(firstContext);
        var secondRepository = new CompanyMembershipRepository(secondContext);

        var results = await Task.WhenAll(
            firstRepository.TryAssignCompanyAsync(targetUserId, companyId, DateTime.UtcNow),
            secondRepository.TryAssignCompanyAsync(targetUserId, companyId, DateTime.UtcNow));

        // SC-IDENTIDAD-31: solo una asignación queda efectiva.
        Assert.Equal(1, results.Count(result => result));

        await using var verificationContext = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var storedUser = await verificationContext.UserAccounts.SingleAsync(user => user.Id == targetUserId);
        Assert.Equal(companyId, storedUser.CompanyId);
    }

    [Fact]
    public async Task Un_usuario_tiene_una_sola_pertenencia_por_proyecto()
    {
        Guid projectId;
        Guid userId;

        await using (var setupContext = SqlServerTestDatabase.CreateContext(fixture.ConnectionString))
        {
            var data = await TestDataFactory.CreateCompanyWithDirectorAsync(setupContext);
            var project = TestDataFactory.CreateProject(data);

            setupContext.Projects.Add(project);
            await setupContext.SaveChangesAsync();

            projectId = project.Id;
            userId = data.Director.Id;
        }

        await using var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var repository = new ProjectMembershipRepository(context);

        var firstMember = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            RoleInProject = "DIRECTOR",
            AssignedByUserId = userId,
            AssignedAtUtc = DateTime.UtcNow
        };

        Assert.True(await repository.TryAddMembershipAsync(firstMember));

        var duplicateMember = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            RoleInProject = "DIRECTOR",
            AssignedByUserId = userId,
            AssignedAtUtc = DateTime.UtcNow
        };

        // SC-PROYECTOS-27 y SC-PROYECTOS-28: la unicidad (project_id, user_id) se traduce en conflicto.
        Assert.False(await repository.TryAddMembershipAsync(duplicateMember));

        await using var verificationContext = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        Assert.Equal(
            1,
            await verificationContext.ProjectMembers.CountAsync(member => member.ProjectId == projectId));
    }
}
