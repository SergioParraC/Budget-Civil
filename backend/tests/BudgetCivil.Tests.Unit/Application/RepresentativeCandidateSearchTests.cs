using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Users.Search;
using BudgetCivil.Application.Users.Search.Repositories;
using BudgetCivil.Application.Users.Search.UseCases;
using FluentAssertions;
using Moq;

namespace BudgetCivil.Tests.Unit.Application;

public sealed class RepresentativeCandidateSearchTests
{
    private readonly Mock<IUserSearchRepository> _repository = new();

    [Theory]
    [InlineData(null, "123")]
    [InlineData("", "123")]
    [InlineData("CC", null)]
    [InlineData("CC", "  ")]
    public async Task El_tipo_y_el_numero_de_documento_son_obligatorios(string? documentType, string? documentNumber)
    {
        var useCase = new SearchRepresentativeCandidatesUseCase(_repository.Object);

        var action = () => useCase.ExecuteAsync(
            Guid.NewGuid(),
            new UserSearchQuery { DocumentType = documentType, DocumentNumber = documentNumber });

        await action.Should().ThrowAsync<ValidationAppException>()
            .Where(exception => exception.Code == "VALIDATION_ERROR");

        _repository.Verify(repository => repository.SearchRepresentativeCandidatesAsync(
            It.IsAny<Guid>(), It.IsAny<UserSearchQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task La_busqueda_por_documento_delega_en_el_repositorio()
    {
        var candidates = new List<UserSearchResult>
        {
            new()
            {
                UserId = Guid.NewGuid(),
                Username = "representante",
                Email = "representante@example.com",
                FirstName = "Ana",
                LastName = "Perez",
                DocumentType = "CC",
                DocumentNumber = "12345678"
            }
        };

        _repository
            .Setup(repository => repository.SearchRepresentativeCandidatesAsync(
                It.IsAny<Guid>(), It.IsAny<UserSearchQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidates);

        var response = await new SearchRepresentativeCandidatesUseCase(_repository.Object).ExecuteAsync(
            Guid.NewGuid(),
            new UserSearchQuery { DocumentType = "CC", DocumentNumber = "12345678", Username = "rep" });

        response.Should().ContainSingle();
        response[0].Username.Should().Be("representante");
    }

    [Fact]
    public async Task El_usuario_inexistente_es_un_conflicto()
    {
        _repository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSearchResult?)null);

        var action = () => new GetUserByIdUseCase(_repository.Object).ExecuteAsync(Guid.NewGuid(), Guid.NewGuid());

        await action.Should().ThrowAsync<ConflictAppException>()
            .Where(exception => exception.Code == "USER_NOT_FOUND");
    }

    [Fact]
    public async Task El_usuario_se_devuelve_para_mostrar_el_representante_actual()
    {
        var user = new UserSearchResult
        {
            UserId = Guid.NewGuid(),
            Username = "representante",
            Email = "representante@example.com",
            FirstName = "Ana",
            LastName = "Perez",
            DocumentType = "CC",
            DocumentNumber = "12345678"
        };
        _repository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var response = await new GetUserByIdUseCase(_repository.Object).ExecuteAsync(Guid.NewGuid(), user.UserId);

        response.Username.Should().Be("representante");
        response.DocumentType.Should().Be("CC");
    }
}
