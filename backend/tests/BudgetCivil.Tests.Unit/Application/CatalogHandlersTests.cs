using BudgetCivil.Application.Catalogs.Commands.CreateUnit;
using BudgetCivil.Application.Catalogs.Queries;
using BudgetCivil.Application.Catalogs.Repositories;
using BudgetCivil.Application.Catalogs.Services;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Domain.Catalogs;
using FluentAssertions;
using Moq;

namespace BudgetCivil.Tests.Unit.Application;

/// <summary>`T-U-28`: alta, recorte, unicidad y consulta del catálogo de unidades.</summary>
public sealed class CatalogHandlersTests
{
    [Fact]
    public async Task Create_recorta_y_guarda_la_unidad()
    {
        var repository = new Mock<IMeasurementUnitRepository>();
        MeasurementUnit? added = null;

        repository
            .Setup(repo => repo.ShortUnitExistsAsync("M2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository
            .Setup(repo => repo.AddAsync(It.IsAny<MeasurementUnit>(), It.IsAny<CancellationToken>()))
            .Callback<MeasurementUnit, CancellationToken>((unit, _) => added = unit)
            .Returns(Task.CompletedTask);
        repository
            .Setup(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new CreateUnitCommandHandler(repository.Object, new CreateUnitCommandValidator());

        var response = await handler.ExecuteAsync(new CreateUnitCommand
        {
            ShortUnit = " M2 ",
            Unit = " Metro cuadrado "
        });

        response.ShortUnit.Should().Be("M2");
        response.Unit.Should().Be("Metro cuadrado");
        added.Should().NotBeNull();
        added!.ShortUnit.Should().Be("M2");
        repository.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_rechaza_una_sigla_repetida_sin_guardar()
    {
        var repository = new Mock<IMeasurementUnitRepository>();
        repository
            .Setup(repo => repo.ShortUnitExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new CreateUnitCommandHandler(repository.Object, new CreateUnitCommandValidator());

        var action = () => handler.ExecuteAsync(new CreateUnitCommand
        {
            ShortUnit = "m2",
            Unit = "Metro cuadrado"
        });

        var exception = await action.Should().ThrowAsync<ConflictAppException>();
        exception.Which.Code.Should().Be(UnitRules.AlreadyExists);
        repository.Verify(
            repo => repo.AddAsync(It.IsAny<MeasurementUnit>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Create_rechaza_datos_invalidos_con_el_codigo_del_catalogo()
    {
        var repository = new Mock<IMeasurementUnitRepository>();
        var handler = new CreateUnitCommandHandler(repository.Object, new CreateUnitCommandValidator());

        var action = () => handler.ExecuteAsync(new CreateUnitCommand
        {
            ShortUnit = new string('x', MeasurementUnit.ShortUnitMaxLength + 1),
            Unit = "Metro cuadrado"
        });

        var exception = await action.Should().ThrowAsync<ValidationAppException>();
        exception.Which.Code.Should().Be(UnitRules.ValidationError);
    }

    [Fact]
    public async Task Get_devuelve_las_unidades_ordenadas_por_sigla()
    {
        var repository = new Mock<IMeasurementUnitRepository>();
        repository
            .Setup(repo => repo.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                MeasurementUnit.Create("UND", "Unidad"),
                MeasurementUnit.Create("M2", "Metro cuadrado")
            ]);

        var handler = new GetUnitsQueryHandler(repository.Object);

        var response = await handler.ExecuteAsync();

        response.Select(unit => unit.ShortUnit).Should().Equal("M2", "UND");
    }
}