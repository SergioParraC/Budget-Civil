using System.Text.Json;
using BudgetCivil.Application.Apus.Dtos;
using BudgetCivil.Application.Wbs.Dtos;
using FluentAssertions;

namespace BudgetCivil.Tests.Unit.Application;

/// <summary>
/// `T-C-05`: contrato v1 de APUs, Matrices, árbol, costos y conteo (`D5`, `D-101`).
/// </summary>
public sealed class ApuContractTests
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void El_detalle_del_apu_usa_camel_case_y_expone_los_campos_de_costo()
    {
        var response = new ApuDetailResponse
        {
            ApuId = Guid.NewGuid(),
            Description = "Excavación",
            Unit = "m3",
            Status = "COMPLETO",
            WbsItem = new WbsItemReferenceDto { Code = "1.1", QuantityWork = 10m, Unit = "m3" },
            Resources =
            [
                new ApuResourceGroupDto
                {
                    ResourceType = "MATERIAL",
                    Items =
                    [
                        new ApuResourceItemDto
                        {
                            ResourceId = Guid.NewGuid(),
                            Description = "Cemento",
                            Unit = "kg",
                            UnitPrice = 1200m,
                            Quantity = 2.5m,
                            Subtotal = 3000m
                        }
                    ]
                }
            ],
            DirectCost = 3000m,
            IndirectCosts = new IndirectCostsDto
            {
                Administration = new AiuComponentDto { Percent = 10m, Amount = 300m },
                Unforeseen = new AiuComponentDto { Percent = 5m, Amount = 150m },
                Profit = new AiuComponentDto { Percent = 8m, Amount = 240m },
                Total = 690m
            },
            Total = 3690m
        };

        var json = JsonSerializer.Serialize(response, WebOptions);

        json.Should().Contain("\"apuId\"");
        json.Should().Contain("\"description\"");
        json.Should().Contain("\"unit\":\"m3\"");
        json.Should().Contain("\"status\":\"COMPLETO\"");
        json.Should().Contain("\"code\":\"1.1\"");
        json.Should().Contain("\"resources\"");
        json.Should().Contain("\"resourceType\":\"MATERIAL\"");
        json.Should().Contain("\"items\"");
        json.Should().Contain("\"subtotal\":3000");
        json.Should().Contain("\"directCost\":3000");
        json.Should().Contain("\"indirectCosts\"");
        json.Should().Contain("\"administration\"");
        json.Should().Contain("\"unforeseen\"");
        json.Should().Contain("\"profit\"");
        json.Should().Contain("\"total\":3690");
        json.Should().NotContain("direct_cost");
        json.Should().NotContain("\"DirectCost\"");
    }

    [Fact]
    public void El_arbol_expone_la_referencia_del_apu_por_item()
    {
        var node = WbsMapper.ToItemNode(
            CreateItem(),
            new BudgetCivil.Application.Wbs.Repositories.ApuSummary(
                Guid.NewGuid(),
                "COMPLETO",
                "Excavación",
                "m3",
                "AQ=="));

        var json = JsonSerializer.Serialize(node, WebOptions);

        json.Should().Contain("\"apu\"");
        json.Should().Contain("\"status\":\"COMPLETO\"");
        json.Should().Contain("\"unitPrice\":null");
        json.Should().Contain("\"rowVersion\":\"AQ==\"");
        json.Should().Contain("\"description\"");
        json.Should().Contain("\"unit\":\"m3\"");
        json.Should().Contain("\"unitCost\":null");
        json.Should().Contain("\"directCost\":null");
    }

    [Fact]
    public void La_matriz_devuelve_recursos_y_costo_directo_sin_aiu()
    {
        var response = new MatrixDetailResponse
        {
            MatrixResourceId = Guid.NewGuid(),
            Description = "Mortero",
            Unit = "m3",
            Status = "POR_COTIZAR",
            Resources =
            [
                new MatrixResourceGroupDto
                {
                    ResourceType = "MATERIAL",
                    Items =
                    [
                        new MatrixComponentItemDto
                        {
                            ComponentId = Guid.NewGuid(),
                            ResourceId = Guid.NewGuid(),
                            Description = "Arena",
                            Unit = "m3",
                            UnitPrice = 5000m,
                            Quantity = 1m,
                            Subtotal = 5000m
                        }
                    ]
                }
            ],
            DirectCost = 5000m
        };

        var json = JsonSerializer.Serialize(response, WebOptions);

        json.Should().Contain("\"matrixResourceId\"");
        json.Should().Contain("\"resources\"");
        json.Should().Contain("\"subtotal\":5000");
        json.Should().Contain("\"directCost\":5000");
        json.Should().NotContain("indirectCosts");
    }

    [Fact]
    public void La_vista_previa_de_copia_compara_unidad_y_precio_del_origen_con_el_destino()
    {
        var response = new ApuCopyPreviewResponse
        {
            SourceApuId = Guid.NewGuid(),
            Items =
            [
                new ApuCopyPreviewItemDto
                {
                    SourceResourceId = Guid.NewGuid(),
                    Description = "Cemento gris",
                    ResourceTypeCode = "MATERIAL",
                    Unit = "kg",
                    UnitPrice = 1200m,
                    Match = "EXACT",
                    RequiresResolution = true,
                    Candidates =
                    [
                        new ApuCopyCandidateDto
                        {
                            ResourceId = Guid.NewGuid(),
                            Description = "Cemento gris",
                            ResourceTypeCode = "MATERIAL",
                            Unit = "bulto",
                            UnitPrice = 50000m,
                            QuotationStatus = "POR_COTIZAR",
                            Comparison = new ApuCopyComparisonDto
                            {
                                UnitMatches = false,
                                UnitPriceMatches = false,
                                UnitPriceDifference = 48800m
                            }
                        }
                    ]
                }
            ]
        };

        var json = JsonSerializer.Serialize(response, WebOptions);

        json.Should().Contain("\"sourceApuId\"");
        json.Should().Contain("\"unitPrice\":1200");
        json.Should().Contain("\"match\":\"EXACT\"");
        json.Should().Contain("\"requiresResolution\":true");
        json.Should().Contain("\"candidates\"");
        json.Should().Contain("\"comparison\"");
        json.Should().Contain("\"unitMatches\":false");
        json.Should().Contain("\"unitPriceMatches\":false");
        json.Should().Contain("\"unitPriceDifference\":48800");
        json.Should().Contain("\"resourceTypeCode\":\"MATERIAL\"");
        json.Should().Contain("\"quotationStatus\":\"POR_COTIZAR\"");
    }

    [Fact]
    public void El_reporte_de_uso_agrupa_por_tipo_e_incluye_precio_unitario_y_total()
    {
        var response = new ResourceUsageResponse
        {
            ProjectId = Guid.NewGuid(),
            Resources =
            [
                new ResourceUsageGroupDto
                {
                    ResourceType = "MATERIAL",
                    ResourceTypeName = "Materiales",
                    Subtotal = 350m,
                    Items =
                    [
                        new ResourceUsageItemDto
                        {
                            ResourceId = Guid.NewGuid(),
                            Description = "Cemento",
                            Unit = "kg",
                            UnitPrice = 1200m,
                            Quantity = 20m,
                            TotalPrice = 24000m
                        }
                    ]
                }
            ],
            TotalCost = 350m
        };

        var json = JsonSerializer.Serialize(response, WebOptions);

        json.Should().Contain("\"projectId\"");
        json.Should().Contain("\"resources\"");
        json.Should().Contain("\"resourceType\":\"MATERIAL\"");
        json.Should().Contain("\"resourceTypeName\":\"Materiales\"");
        json.Should().Contain("\"unitPrice\":1200");
        json.Should().Contain("\"quantity\":20");
        json.Should().Contain("\"totalPrice\":24000");
        json.Should().Contain("\"subtotal\":350");
        json.Should().Contain("\"totalCost\":350");
        json.Should().NotContain("\"items\":[]");
    }

    [Fact]
    public void La_respuesta_plana_de_item_y_apu_expone_ambos_lados()
    {
        var json = JsonSerializer.Serialize(
            new WbsItemApuResponse
            {
                Id = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                NodeType = "ITEM",
                Code = "1.1",
                QuantityWork = 10m,
                RowVersion = "item-rv",
                ApuId = Guid.NewGuid(),
                ApuStatus = "COMPLETO",
                ApuRowVersion = "apu-rv",
                Description = "Excavacion",
                Unit = "m3",
                Resources =
                [
                    new ApuResourceGroupDto
                    {
                        ResourceType = "MATERIAL",
                        Items =
                        [
                            new ApuResourceItemDto
                            {
                                LineId = Guid.NewGuid(),
                                ResourceId = Guid.NewGuid(),
                                Description = "Cemento",
                                Unit = "kg",
                                UnitPrice = 1200m,
                                Quantity = 2m,
                                Subtotal = 2400m
                            }
                        ]
                    }
                ],
                DirectCost = 2400m,
                IndirectCosts = new IndirectCostsDto { Total = 360m },
                Total = 2760m
            },
            WebOptions);

        json.Should().Contain("\"nodeType\":\"ITEM\"");
        json.Should().Contain("\"code\":\"1.1\"");
        json.Should().Contain("\"quantityWork\":10");
        json.Should().Contain("\"rowVersion\":\"item-rv\"");
        json.Should().Contain("\"apuId\"");
        json.Should().Contain("\"apuStatus\":\"COMPLETO\"");
        json.Should().Contain("\"apuRowVersion\":\"apu-rv\"");
        json.Should().Contain("\"description\":\"Excavacion\"");
        json.Should().Contain("\"unit\":\"m3\"");
        json.Should().Contain("\"resources\"");
        json.Should().Contain("\"lineId\"");
        json.Should().Contain("\"directCost\":2400");
        json.Should().Contain("\"total\":2760");
        json.Should().Contain("\"children\":[]");
    }

    private static BudgetCivil.Domain.Wbs.WbsItem CreateItem()
    {
        var projectId = Guid.NewGuid();
        return BudgetCivil.Domain.Wbs.WbsItem.Create(
            projectId,
            null,
            BudgetCivil.Domain.Wbs.WbsCode.Create(
                projectId,
                "1.1",
                BudgetCivil.Domain.Wbs.WbsNodeType.Item),
            10m,
            0,
            Guid.NewGuid(),
            DateTime.UtcNow);
    }
}
