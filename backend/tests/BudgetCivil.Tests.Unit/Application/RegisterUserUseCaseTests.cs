using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Common.Notifications;
using BudgetCivil.Application.Common.Security;
using BudgetCivil.Application.Catalogs.Repositories;
using BudgetCivil.Application.Users.Auth.Dtos;
using BudgetCivil.Application.Users.Auth.Repositories;
using BudgetCivil.Application.Users.Auth.UseCases;
using BudgetCivil.Application.Users.Auth.Validators;
using BudgetCivil.Domain.Users.Entities;
using FluentAssertions;
using Moq;

namespace BudgetCivil.Tests.Unit.Application;

public sealed class RegisterUserUseCaseTests
{
    private readonly Mock<IAuthRepository> _authRepository = new();
    private readonly Mock<IEmailVerificationTokenRepository> _emailVerificationRepository = new();
    private readonly Mock<IGeographicCatalogRepository> _catalogRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly Guid _roleId = Guid.NewGuid();
    private string? _sentBody;

    public RegisterUserUseCaseTests()
    {
        _catalogRepository
            .Setup(repository => repository.IsValidActiveLocationAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _authRepository
            .Setup(repository => repository.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _authRepository
            .Setup(repository => repository.UsernameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _authRepository
            .Setup(repository => repository.GetRoleByCodeAsync("PRESUPUESTADOR", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Role { Id = _roleId, Code = "PRESUPUESTADOR", Name = "Presupuestador", IsActive = true });
        _authRepository
            .Setup(repository => repository.AddUserAsync(It.IsAny<UserAccount>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _emailVerificationRepository
            .Setup(repository => repository.AddTokenAsync(It.IsAny<EmailVerificationToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _emailVerificationRepository
            .Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _passwordHasher
            .Setup(hasher => hasher.HashPassword("ClaveSegura1!"))
            .Returns("hash-generado");

        _emailSender
            .Setup(sender => sender.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, _, body, _) => _sentBody = body)
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task El_autorregistro_persiste_la_identidad_con_rol_presupuestador_y_sin_compania()
    {
        var request = RegisterUserRequestValidatorTests.CreateRequest();
        var useCase = CreateUseCase();

        var response = await useCase.ExecuteAsync(request);

        response.AssignedRole.Should().Be("Presupuestador");
        response.CompanyId.Should().BeNull();
        // D-80: la cuenta no se habilita hasta verificar el correo.
        response.AccountEnabled.Should().BeFalse();

        _authRepository.Verify(
            repository => repository.AddUserAsync(
                It.Is<UserAccount>(user =>
                    user.CompanyId == null &&
                    user.RoleId == _roleId &&
                    user.Email == request.Email &&
                    user.Username == request.Username &&
                    user.PasswordHash == "hash-generado" &&
                    user.AccountEnabled == false &&
                    user.FirstName == "Ana" &&
                    user.MiddleName == "Maria" &&
                    user.LastName == "Perez" &&
                    user.SecondLastName == "Gomez" &&
                    user.DocumentType == "CC" &&
                    user.DocumentNumber == "12345678" &&
                    user.BirthDate == new DateOnly(1995, 5, 20) &&
                    user.CountryId == request.CountryId &&
                    user.DepartmentId == request.DepartmentId &&
                    user.MunicipalityId == request.MunicipalityId),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _emailSender.Verify(
            sender => sender.SendAsync(
                request.Email,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task El_autorregistro_guarda_el_hash_del_token_y_envia_el_token_en_el_correo()
    {
        var request = RegisterUserRequestValidatorTests.CreateRequest();

        await CreateUseCase().ExecuteAsync(request);

        _sentBody.Should().NotBeNullOrWhiteSpace();
        var rawToken = _sentBody!.Split(':').Last().Trim();
        rawToken.Should().NotBeNullOrWhiteSpace();

        _emailVerificationRepository.Verify(
            repository => repository.AddTokenAsync(
                It.Is<EmailVerificationToken>(token =>
                    token.TokenHash == TokenHash.Compute(rawToken) &&
                    token.ExpiresAtUtc > DateTime.UtcNow),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // El correo no contiene la contraseña ni su hash.
        _sentBody.Should().NotContain("ClaveSegura1!").And.NotContain("hash-generado");
    }

    [Fact]
    public async Task El_autorregistro_rechaza_un_correo_duplicado()
    {
        _authRepository
            .Setup(repository => repository.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var action = () => CreateUseCase().ExecuteAsync(RegisterUserRequestValidatorTests.CreateRequest());

        await action.Should().ThrowAsync<ConflictAppException>()
            .Where(exception => exception.Code == "EMAIL_ALREADY_EXISTS");

        _authRepository.Verify(
            repository => repository.AddUserAsync(It.IsAny<UserAccount>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task El_autorregistro_rechaza_un_nombre_de_usuario_duplicado()
    {
        _authRepository
            .Setup(repository => repository.UsernameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var action = () => CreateUseCase().ExecuteAsync(RegisterUserRequestValidatorTests.CreateRequest());

        await action.Should().ThrowAsync<ConflictAppException>()
            .Where(exception => exception.Code == "USERNAME_ALREADY_EXISTS");
    }

    [Fact]
    public async Task El_autorregistro_falla_si_el_rol_por_defecto_no_esta_disponible()
    {
        _authRepository
            .Setup(repository => repository.GetRoleByCodeAsync("PRESUPUESTADOR", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        var action = () => CreateUseCase().ExecuteAsync(RegisterUserRequestValidatorTests.CreateRequest());

        await action.Should().ThrowAsync<ConflictAppException>()
            .Where(exception => exception.Code == "DEFAULT_ROLE_NOT_AVAILABLE");
    }

    [Fact]
    public async Task El_autorregistro_valida_que_la_ubicacion_exista_y_sea_activa()
    {
        _catalogRepository
            .Setup(repository => repository.IsValidActiveLocationAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var action = () => CreateUseCase().ExecuteAsync(RegisterUserRequestValidatorTests.CreateRequest());

        await action.Should().ThrowAsync<ValidationAppException>()
            .Where(exception => exception.Code == "VALIDATION_ERROR");
    }

    [Fact]
    public async Task El_autorregistro_valida_la_solicitud_antes_de_persistir()
    {
        var invalidRequest = RegisterUserRequestValidatorTests.CreateRequest(email: "sin-arroba");

        var action = () => CreateUseCase().ExecuteAsync(invalidRequest);

        await action.Should().ThrowAsync<ValidationAppException>();

        _authRepository.Verify(
            repository => repository.AddUserAsync(It.IsAny<UserAccount>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private RegisterUserUseCase CreateUseCase() => new(
        _authRepository.Object,
        _emailVerificationRepository.Object,
        _catalogRepository.Object,
        _passwordHasher.Object,
        _emailSender.Object,
        new RegisterUserRequestValidator());
}
