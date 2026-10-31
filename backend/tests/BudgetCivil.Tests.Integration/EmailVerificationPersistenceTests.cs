using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Common.Security;
using BudgetCivil.Application.Users.Auth.UseCases;
using BudgetCivil.Domain.Users.Entities;
using BudgetCivil.Infrastructure.Persistence.Repositories;
using BudgetCivil.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Tests.Integration;

/// <summary>D-80: el token de verificación se persiste como hash y habilita la cuenta una sola vez.</summary>
public sealed class EmailVerificationPersistenceTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task El_token_se_persiste_y_habilita_la_cuenta_una_sola_vez()
    {
        Guid userId;

        await using (var setupContext = SqlServerTestDatabase.CreateContext(fixture.ConnectionString))
        {
            var role = await setupContext.Roles.SingleAsync(existingRole => existingRole.Code == "PRESUPUESTADOR");
            var suffix = Guid.NewGuid().ToString("N");

            var user = new UserAccount
            {
                Id = Guid.NewGuid(),
                RoleId = role.Id,
                Email = $"verificacion.{suffix}@example.com",
                Username = $"verificacion_{suffix}",
                PasswordHash = "hash",
                AccountEnabled = false,
                FirstName = "Verificacion",
                LastName = "Prueba",
                DocumentType = "CC",
                DocumentNumber = $"DOC-{suffix}",
                BirthDate = new DateOnly(1993, 2, 1)
            };

            setupContext.UserAccounts.Add(user);
            await setupContext.SaveChangesAsync();
            userId = user.Id;
        }

        var rawToken = TokenHash.CreateRawToken();

        await using (var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString))
        {
            var repository = new EmailVerificationTokenRepository(context);
            await repository.AddTokenAsync(new EmailVerificationToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = TokenHash.Compute(rawToken),
                ExpiresAtUtc = DateTime.UtcNow.AddHours(24),
                CreatedAtUtc = DateTime.UtcNow
            });
            await repository.SaveChangesAsync();
        }

        await using (var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString))
        {
            var repository = new EmailVerificationTokenRepository(context);
            var useCase = new ConfirmEmailVerificationUseCase(repository);

            await useCase.ExecuteAsync(rawToken);

            // SC-IDENTIDAD-15: el segundo intento con el mismo token es rechazado.
            await Assert.ThrowsAsync<ValidationAppException>(() => useCase.ExecuteAsync(rawToken));
        }

        await using var verificationContext = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var storedUser = await verificationContext.UserAccounts.SingleAsync(user => user.Id == userId);
        var storedToken = await verificationContext.EmailVerificationTokens.SingleAsync(token => token.UserId == userId);

        Assert.True(storedUser.AccountEnabled);
        Assert.NotNull(storedToken.ConsumedAtUtc);
        Assert.NotEqual(rawToken, storedToken.TokenHash);
    }
}
