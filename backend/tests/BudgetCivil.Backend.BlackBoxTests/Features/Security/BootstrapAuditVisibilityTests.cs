using System.Net;
using System.Net.Http.Json;
using BudgetCivil.Backend.BlackBoxTests.Infrastructure;
using FluentAssertions;

namespace BudgetCivil.Backend.BlackBoxTests.Features.Security;

public sealed class BootstrapAuditVisibilityTests
{
    [Fact]
    public async Task Bootstrap_Audit_Is_Visible_For_Administrador()
    {
        using var factory = new TestWebApplicationFactory();
        var anonymousClient = factory.CreateClient();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var bootstrapRequest = new
        {
            admin_profile = new
            {
                primer_nombre = "Linus",
                primer_apellido = "Torvalds",
                tipo_documento = "CC",
                numero_documento = $"LT{unique}",
                email = $"linus.{unique}@mail.local",
                username = $"linus_{unique}",
                password = "Password1!"
            },
            company = new
            {
                business_name = $"Bootstrap Company {unique}",
                tax_id = $"TX{unique}"
            }
        };

        var bootstrapResponse = await anonymousClient.PostAsJsonAsync("/api/v1/admin/bootstrap/first-administrator", bootstrapRequest);
        bootstrapResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var seededAdmin = await TestDataSeeder.SeedUserAsync(factory, "ADMINISTRADOR", "Password1!");

        var adminClient = new AuthenticatedClientBuilder(factory)
            .WithUser(seededAdmin.UserId, "ADMINISTRADOR")
            .Build();

        var response = await adminClient.GetAsync("/api/v1/audit/bootstrap");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("BOOTSTRAP");
    }

    [Fact]
    public async Task Bootstrap_Audit_Is_Forbidden_For_Non_Administrador_Roles()
    {
        using var factory = new TestWebApplicationFactory();
        var anonymousClient = factory.CreateClient();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var bootstrapRequest = new
        {
            admin_profile = new
            {
                primer_nombre = "Margaret",
                primer_apellido = "Hamilton",
                tipo_documento = "CC",
                numero_documento = $"MH{unique}",
                email = $"margaret.{unique}@mail.local",
                username = $"margaret_{unique}",
                password = "Password1!"
            },
            company = new
            {
                business_name = $"Bootstrap Company {unique}",
                tax_id = $"TY{unique}"
            }
        };

        var bootstrapResponse = await anonymousClient.PostAsJsonAsync("/api/v1/admin/bootstrap/first-administrator", bootstrapRequest);
        bootstrapResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var auditorUser = await TestDataSeeder.SeedUserAsync(factory, "AUDITOR", "Password1!");

        var auditorClient = new AuthenticatedClientBuilder(factory)
            .WithUser(auditorUser.UserId, "AUDITOR")
            .Build();

        var response = await auditorClient.GetAsync("/api/v1/audit/bootstrap");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
