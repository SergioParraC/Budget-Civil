using System.Reflection;
using NetArchTest.Rules;

namespace BudgetCivil.Tests.Architecture;

/// <summary>
/// Reglas de dependencia de `§3.4.6` (`T-A-02`). Las reglas que requieren criterio ("Infrastructure no
/// contiene decisiones de negocio" y "un módulo no navega entidades de otro") siguen siendo revisión
/// manual: no se pueden expresar sobre referencias entre tipos.
/// </summary>
public sealed class LayerDependencyTests
{
    private static readonly Assembly DomainAssembly = typeof(BudgetCivil.Domain.Projects.Project).Assembly;

    private static readonly Assembly ApplicationAssembly =
        typeof(BudgetCivil.Application.DependencyInjection).Assembly;

    private static readonly Assembly ApiAssembly =
        typeof(BudgetCivil.Api.Controllers.ProjectsController).Assembly;

    [Fact]
    public void Domain_no_depende_de_EntityFrameworkCore_ni_de_ASP_NET()
    {
        AssertNoDependency(
            DomainAssembly,
            "Microsoft.EntityFrameworkCore",
            "Microsoft.Data.SqlClient",
            "Microsoft.AspNetCore",
            "Microsoft.Extensions.DependencyInjection");
    }

    [Fact]
    public void Domain_no_depende_de_las_otras_capas()
    {
        AssertNoDependency(
            DomainAssembly,
            "BudgetCivil.Application",
            "BudgetCivil.Infrastructure",
            "BudgetCivil.Api");
    }

    [Fact]
    public void Application_no_depende_de_EntityFrameworkCore()
    {
        AssertNoDependency(
            ApplicationAssembly,
            "Microsoft.EntityFrameworkCore",
            "Microsoft.Data.SqlClient",
            typeof(Microsoft.EntityFrameworkCore.DbContext).FullName!);
    }

    [Fact]
    public void Application_no_depende_de_Infrastructure_ni_de_Api()
    {
        AssertNoDependency(
            ApplicationAssembly,
            "BudgetCivil.Infrastructure",
            "BudgetCivil.Api");
    }

    [Fact]
    public void Api_no_accede_a_la_persistencia()
    {
        AssertNoDependency(
            ApiAssembly,
            "Microsoft.EntityFrameworkCore",
            "Microsoft.Data.SqlClient",
            typeof(Microsoft.EntityFrameworkCore.DbContext).FullName!,
            typeof(BudgetCivil.Infrastructure.Persistence.BudgetCivilDbContext).FullName!);
    }

    [Fact]
    public void Api_no_usa_los_repositorios_de_Application()
    {
        var repositoryInterfaceNames = ApplicationAssembly
            .GetTypes()
            .Where(type => type.IsInterface &&
                type.Namespace is not null &&
                (type.Namespace.EndsWith(".Repositories", StringComparison.Ordinal) ||
                 type.Namespace == "BudgetCivil.Application.Repositories"))
            .Select(type => type.FullName!)
            .ToArray();

        Assert.NotEmpty(repositoryInterfaceNames);

        AssertNoDependency(ApiAssembly, repositoryInterfaceNames);
    }

    [Fact]
    public void Los_controladores_no_dependen_de_repositorios_ni_de_persistencia()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That()
            .ResideInNamespace("BudgetCivil.Api.Controllers")
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.Data.SqlClient",
                "BudgetCivil.Infrastructure.Persistence")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"los controladores no deben acceder a persistencia: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    private static void AssertNoDependency(Assembly assembly, params string[] dependencies)
    {
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(dependencies)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"dependencias prohibidas en {assembly.GetName().Name}: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
