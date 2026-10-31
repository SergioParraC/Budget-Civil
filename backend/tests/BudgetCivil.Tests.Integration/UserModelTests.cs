using BudgetCivil.Domain.Users.Entities;
using BudgetCivil.Infrastructure.Persistence;
using BudgetCivil.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using BudgetCivil.Infrastructure.Security;

namespace BudgetCivil.Tests.Integration;

/// <summary>T-I-28: `User` absorbe el perfil y el número de documento deja de ser único (`D-78`, `D-77`).</summary>
public sealed class UserModelTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task Dos_cuentas_con_el_mismo_documento_conviven()
    {
        await using var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);

        var role = await context.Roles.SingleAsync(existingRole => existingRole.Code == "PRESUPUESTADOR");
        var documentNumber = $"DOC-{Guid.NewGuid():N}";

        var first = CreateUser(role.Id, documentNumber);
        var second = CreateUser(role.Id, documentNumber);

        context.UserAccounts.AddRange(first, second);
        await context.SaveChangesAsync();

        await using var verificationContext = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var stored = await verificationContext.UserAccounts
            .Where(user => user.DocumentNumber == documentNumber)
            .ToListAsync();

        Assert.Equal(2, stored.Count);
        Assert.All(stored, user => Assert.Null(user.CompanyId));

        // D-78: el perfil vive en la misma fila que la cuenta.
        Assert.All(stored, user =>
        {
            Assert.Equal("Prueba", user.FirstName);
            Assert.Equal("CC", user.DocumentType);
            Assert.NotEqual(default, user.BirthDate);
        });
    }

    [Fact]
    public async Task El_correo_y_el_nombre_de_usuario_siguen_siendo_unicos()
    {
        await using var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);

        var role = await context.Roles.SingleAsync(existingRole => existingRole.Code == "AUDITOR");
        var email = $"unico.{Guid.NewGuid():N}@example.com";
        var username = $"unico_{Guid.NewGuid():N}";

        context.UserAccounts.Add(CreateUser(role.Id, $"DOC-{Guid.NewGuid():N}", email, username));
        await context.SaveChangesAsync();

        context.UserAccounts.Add(CreateUser(role.Id, $"DOC-{Guid.NewGuid():N}", email, username));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private static UserAccount CreateUser(Guid roleId, string documentNumber, string? email = null, string? username = null)
    {
        var suffix = Guid.NewGuid().ToString("N");

        return new UserAccount
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            Email = email ?? $"usuario.{suffix}@example.com",
            Username = username ?? $"usuario_{suffix}",
            PasswordHash = "hash",
            AccountEnabled = true,
            FirstName = "Prueba",
            LastName = "Documento",
            DocumentType = "CC",
            DocumentNumber = documentNumber,
            BirthDate = new DateOnly(1992, 3, 4)
        };
    }
}
