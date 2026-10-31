using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Common.Security;
using BudgetCivil.Application.Users.Auth.Repositories;
using BudgetCivil.Application.Users.Auth.UseCases;
using BudgetCivil.Domain.Users.Entities;
using FluentAssertions;
using Moq;

namespace BudgetCivil.Tests.Unit.Application;

public sealed class ConfirmEmailVerificationUseCaseTests
{
    private readonly Mock<IEmailVerificationTokenRepository> _repository = new();

    [Fact]
    public async Task Un_token_valido_habilita_la_cuenta_y_se_consume()
    {
        var rawToken = TokenHash.CreateRawToken();
        var user = CreateUser(accountEnabled: false);
        var token = CreateToken(user, rawToken);
        _repository
            .Setup(repository => repository.FindTokenByHashAsync(TokenHash.Compute(rawToken), It.IsAny<CancellationToken>()))
            .ReturnsAsync(token);
        _repository
            .Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await new ConfirmEmailVerificationUseCase(_repository.Object).ExecuteAsync(rawToken);

        user.AccountEnabled.Should().BeTrue();
        token.ConsumedAtUtc.Should().NotBeNull();
        _repository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Un_token_vacio_es_un_error_de_validacion(string? rawToken)
    {
        var action = () => new ConfirmEmailVerificationUseCase(_repository.Object).ExecuteAsync(rawToken!);

        await action.Should().ThrowAsync<ValidationAppException>()
            .Where(exception => exception.Code == "VALIDATION_ERROR");
    }

    [Fact]
    public async Task Un_token_inexistente_es_rechazado()
    {
        _repository
            .Setup(repository => repository.FindTokenByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailVerificationToken?)null);

        var action = () => new ConfirmEmailVerificationUseCase(_repository.Object).ExecuteAsync("token-inexistente");

        await action.Should().ThrowAsync<ValidationAppException>()
            .Where(exception => exception.Code == "EMAIL_VERIFICATION_TOKEN_INVALID");
    }

    [Fact]
    public async Task Un_token_consumido_o_caducado_es_rechazado()
    {
        var rawToken = TokenHash.CreateRawToken();
        var user = CreateUser(accountEnabled: false);

        var consumed = CreateToken(user, rawToken);
        consumed.ConsumedAtUtc = DateTime.UtcNow;

        var expired = CreateToken(user, rawToken);
        expired.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);

        foreach (var token in new[] { consumed, expired })
        {
            _repository
                .Setup(repository => repository.FindTokenByHashAsync(TokenHash.Compute(rawToken), It.IsAny<CancellationToken>()))
                .ReturnsAsync(token);

            var action = () => new ConfirmEmailVerificationUseCase(_repository.Object).ExecuteAsync(rawToken);

            await action.Should().ThrowAsync<ValidationAppException>()
                .Where(exception => exception.Code == "EMAIL_VERIFICATION_TOKEN_INVALID");
        }

        user.AccountEnabled.Should().BeFalse();
        _repository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static UserAccount CreateUser(bool accountEnabled) => new()
    {
        Id = Guid.NewGuid(),
        AccountEnabled = accountEnabled,
        Role = new Role { Id = Guid.NewGuid(), Code = "PRESUPUESTADOR", Name = "Presupuestador", IsActive = true }
    };

    private static EmailVerificationToken CreateToken(UserAccount user, string rawToken) => new()
    {
        Id = Guid.NewGuid(),
        UserId = user.Id,
        UserAccount = user,
        TokenHash = TokenHash.Compute(rawToken),
        ExpiresAtUtc = DateTime.UtcNow.AddHours(24),
        CreatedAtUtc = DateTime.UtcNow
    };
}
