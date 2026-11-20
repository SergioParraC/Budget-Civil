using System.Text.Json;
using BudgetCivil.Application.Resources.Commands.ChangeResourceQuotationStatus;
using FluentAssertions;

namespace BudgetCivil.Tests.Unit.Application;

/// <summary>`T-C-07`: contrato del cambio masivo de cotización (`D-11`, `D-99`).</summary>
public sealed class QuotationContractTests
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void El_cuerpo_acepta_los_tres_alcances_del_contrato()
    {
        var selection = JsonSerializer.Deserialize<ChangeResourceQuotationStatusCommand>(
            """{"scope":"SELECTION","resourceIds":["6f1c1d18-2a9a-4f6f-9f2f-0f2c2f1c1d18"]}""",
            WebOptions);
        var filter = JsonSerializer.Deserialize<ChangeResourceQuotationStatusCommand>(
            """{"scope":"FILTER","filter":{"description":"cemento","quotationStatus":"POR_COTIZAR","minPrice":0,"maxPrice":100000}}""",
            WebOptions);
        var all = JsonSerializer.Deserialize<ChangeResourceQuotationStatusCommand>(
            """{"scope":"ALL"}""",
            WebOptions);

        selection!.ResourceIds.Should().ContainSingle();
        selection.Filter.Should().BeNull();
        filter!.Filter!.Description.Should().Be("cemento");
        filter.Filter.QuotationStatus.Should().Be("POR_COTIZAR");
        filter.Filter.MinPrice.Should().Be(0m);
        filter.Filter.MaxPrice.Should().Be(100000m);
        all!.Scope.Should().Be("ALL");
    }

    [Fact]
    public void La_respuesta_expone_el_resumen_por_insumo_en_camel_case()
    {
        var response = new ChangeResourceQuotationStatusResponse
        {
            ProjectId = Guid.NewGuid(),
            QuotationStatus = "COTIZADO",
            Requested = 300,
            Updated = 294,
            AlreadyQuoted = 0,
            Skipped =
            [
                new ResourceQuotationSkippedDto
                {
                    ResourceId = Guid.NewGuid(),
                    Code = "RESOURCE_QUOTATION_INCOMPLETE"
                }
            ],
            PendingResources = 6,
            AllQuoted = false
        };

        var json = JsonSerializer.Serialize(response, WebOptions);

        json.Should().Contain("\"projectId\"");
        json.Should().Contain("\"quotationStatus\":\"COTIZADO\"");
        json.Should().Contain("\"requested\":300");
        json.Should().Contain("\"updated\":294");
        json.Should().Contain("\"alreadyQuoted\":0");
        json.Should().Contain("\"skipped\"");
        json.Should().Contain("\"resourceId\"");
        json.Should().Contain("\"code\":\"RESOURCE_QUOTATION_INCOMPLETE\"");
        json.Should().Contain("\"pendingResources\":6");
        json.Should().Contain("\"allQuoted\":false");
        json.Should().NotContain("pending_resources");
    }
}
