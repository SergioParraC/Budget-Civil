using BudgetCivil.Application.Catalogs.Repositories;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Users.Bootstrap.Dtos;
using BudgetCivil.Application.Users.Bootstrap.Repositories;
using BudgetCivil.Application.Users.Bootstrap.UseCases;
using BudgetCivil.Application.Users.Bootstrap.Validators;
using FluentAssertions;
using Moq;

namespace BudgetCivil.Tests.Unit.Application;

public sealed class BootstrapFirstAdministratorUseCaseTests
{
    private readonly Mock<IBootstrapRepository> _repository = new();
    private readonly Mock<IGeographicCatalogRepository> _catalog = new();
    private readonly Guid _administratorId = Guid.NewGuid();
    private readonly Guid _auditEventId = Guid.NewGuid();

    public BootstrapFirstAdministratorUseCaseTests()
    {
        _catalog
            .Setup(repository => repository.IsValidActiveLocationAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repository.Setup(repository => repository.AnyAdministratorExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repository.Setup(repository => repository.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repository.Setup(repository => repository.UsernameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repository.Setup(repository => repository.CreateFirstAdministratorAsync(
                It.IsAny<BootstrapRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((_administratorId, _auditEventId));
    }

    [Fact]
    public void El_request_del_bootstrap_ya_no_exige_compania()
    {
        // SC-IDENTIDAD-69: el Administrador se crea sin compañía.
        typeof(BootstrapRequest).GetProperty("Company").Should().BeNull();
    }

    [Fact]
    public async Task Crea_el_primer_administrador_sin_compania_ni_residencia()
    {
        var request = CreateRequest();

        var response = await CreateUseCase().ExecuteAsync(request);

        response.AdministratorCreated.Should().BeTrue();
        response.BootstrapEnabled.Should().BeFalse();
        response.AdministratorId.Should().Be(_administratorId);
        response.AuditEventId.Should().Be(_auditEventId);

        _repository.Verify(repository => repository.CreateFirstAdministratorAsync(
            It.Is<BootstrapRequest>(created =>
                created.AdminProfile.Email == request.AdminProfile.Email &&
                created.AdminProfile.Username == request.AdminProfile.Username),
            It.IsAny<CancellationToken>()), Times.Once);

        // Sin residencia informada no se consulta el catálogo geográfico.
        _catalog.Verify(repository => repository.IsValidActiveLocationAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Valida_la_residencia_cuando_se_informa()
    {
        var countryId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();
        var municipalityId = Guid.NewGuid();
        var request = CreateRequest(countryId, departmentId, municipalityId);

        await CreateUseCase().ExecuteAsync(request);

        _catalog.Verify(repository => repository.IsValidActiveLocationAsync(
            countryId, departmentId, municipalityId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Rechaza_una_residencia_incompleta()
    {
        var request = CreateRequest(countryId: Guid.NewGuid());

        var action = () => CreateUseCase().ExecuteAsync(request);

        await action.Should().ThrowAsync<ValidationAppException>();

        _repository.Verify(repository => repository.CreateFirstAdministratorAsync(
            It.IsAny<BootstrapRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Rechaza_el_segundo_bootstrap()
    {
        _repository.Setup(repository => repository.AnyAdministratorExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var action = () => CreateUseCase().ExecuteAsync(CreateRequest());

        await action.Should().ThrowAsync<ConflictAppException>()
            .Where(exception => exception.Code == "BOOTSTRAP_ALREADY_COMPLETED");

        _repository.Verify(repository => repository.CreateFirstAdministratorAsync(
            It.IsAny<BootstrapRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Exige_la_fecha_de_nacimiento()
    {
        var request = CreateRequest(birthDate: DateOnly.MinValue);

        var action = () => CreateUseCase().ExecuteAsync(request);

        await action.Should().ThrowAsync<ValidationAppException>();
    }

    private BootstrapFirstAdministratorUseCase CreateUseCase() => new(
        _repository.Object,
        _catalog.Object,
        new BootstrapRequestValidator());

    private static BootstrapRequest CreateRequest(
        Guid? countryId = null,
        Guid? departmentId = null,
        Guid? municipalityId = null,
        DateOnly? birthDate = null) => new()
    {
        AdminProfile = new BootstrapAdminProfileRequest
        {
            FirstName = "Administrador",
            LastName = "Inicial",
            DocumentType = "CC",
            DocumentNumber = "1000000001",
            BirthDate = birthDate ?? new DateOnly(1990, 1, 1),
            Email = "admin@budgetcivil.local",
            Username = "admin",
            Password = "ClaveSegura1!",
            CountryId = countryId,
            DepartmentId = departmentId,
            MunicipalityId = municipalityId
        }
    };
}
