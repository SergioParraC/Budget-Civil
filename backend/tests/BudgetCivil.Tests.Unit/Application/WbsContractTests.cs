using System.Text.Json;
using BudgetCivil.Application.Wbs.Commands.CreateWbsItem;
using BudgetCivil.Application.Wbs.Commands.DeleteWbsChapter;
using BudgetCivil.Application.Wbs.Commands.MoveWbsNode;
using BudgetCivil.Application.Wbs.Commands.ReorderWbsNodes;
using BudgetCivil.Application.Wbs.Dtos;
using BudgetCivil.Application.Wbs.Queries;
using FluentAssertions;

namespace BudgetCivil.Tests.Unit.Application;

/// <summary>
/// `T-C-04`: contrato v1 del EDT (`D-41`, `D-55`, `D-99`…`D-101`). Comprueba `camelCase`, los campos
/// publicados, la paginación, `move`, `activity`, `codes/validate`, `cascade` y `rowVersion` en baja.
/// </summary>
public sealed class WbsContractTests
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void El_nodo_del_arbol_usa_camel_case_y_expone_los_campos_del_contrato()
    {
        var json = JsonSerializer.Serialize(CreateNode(), WebOptions);

        json.Should().Contain("\"id\"");
        json.Should().Contain("\"projectId\"");
        json.Should().Contain("\"nodeType\":\"CHAPTER\"");
        json.Should().Contain("\"code\":\"CIM.1\"");
        json.Should().Contain("\"parentChapterId\"");
        json.Should().Contain("\"name\"");
        json.Should().Contain("\"quantityWork\"");
        json.Should().Contain("\"orderIndex\"");
        json.Should().Contain("\"rowVersion\"");
        json.Should().Contain("\"lastModifiedBy\"");
        json.Should().Contain("\"lastModifiedAt\"");
        json.Should().Contain("\"children\"");
        json.Should().NotContain("project_id");
        json.Should().NotContain("\"ProjectId\"");
    }

    [Fact]
    public void La_busqueda_expone_page_pageSize_y_totalItems()
    {
        var json = JsonSerializer.Serialize(
            new SearchWbsResponse { Page = 1, PageSize = 50, TotalItems = 2 },
            WebOptions);

        json.Should().Contain("\"page\":1");
        json.Should().Contain("\"pageSize\":50");
        json.Should().Contain("\"totalItems\":2");
        json.Should().Contain("\"items\"");
    }

    [Fact]
    public void La_validacion_de_codigo_expone_isValid_isAvailable_y_reason()
    {
        // SC-EDT-58…61.
        var json = JsonSerializer.Serialize(
            new ValidateWbsCodeResponse
            {
                Code = "cim.1",
                IsValid = true,
                IsAvailable = false,
                Reason = "WBS_CODE_DUPLICATED"
            },
            WebOptions);

        json.Should().Contain("\"code\":\"cim.1\"");
        json.Should().Contain("\"isValid\":true");
        json.Should().Contain("\"isAvailable\":false");
        json.Should().Contain("\"reason\":\"WBS_CODE_DUPLICATED\"");
    }

    [Fact]
    public void La_actividad_expone_lastActivity_y_participants()
    {
        // SC-EDT-73 y SC-EDT-74.
        var empty = JsonSerializer.Serialize(
            new GetWbsActivityResponse { ProjectId = Guid.NewGuid() },
            WebOptions);

        empty.Should().Contain("\"lastActivity\":null");
        empty.Should().Contain("\"participants\":[]");

        var filled = JsonSerializer.Serialize(
            new GetWbsActivityResponse
            {
                ProjectId = Guid.NewGuid(),
                LastActivity = new WbsActivityParticipantDto
                {
                    UserId = Guid.NewGuid(),
                    UserName = "director",
                    LastModifiedAt = DateTime.UtcNow
                },
                Participants =
                [
                    new WbsActivityParticipantDto
                    {
                        UserId = Guid.NewGuid(),
                        UserName = "director",
                        LastModifiedAt = DateTime.UtcNow
                    }
                ]
            },
            WebOptions);

        filled.Should().Contain("\"lastActivity\"");
        filled.Should().Contain("\"participants\"");
        filled.Should().Contain("\"userName\"");
        filled.Should().Contain("\"lastModifiedAt\"");
    }

    [Fact]
    public void Los_comandos_de_movimiento_baja_y_reorden_exponen_el_contrato()
    {
        var move = JsonSerializer.Serialize(
            new MoveWbsNodeCommand
            {
                NodeType = "ITEM",
                NodeId = Guid.NewGuid(),
                TargetParentChapterId = Guid.NewGuid(),
                TargetIndex = 1,
                RowVersion = "AQ=="
            },
            WebOptions);

        move.Should().Contain("\"nodeType\":\"ITEM\"");
        move.Should().Contain("\"nodeId\"");
        move.Should().Contain("\"targetParentChapterId\"");
        move.Should().Contain("\"targetIndex\":1");
        move.Should().Contain("\"rowVersion\":\"AQ==\"");

        var delete = JsonSerializer.Serialize(
            new DeleteWbsChapterCommand { Cascade = true, RowVersion = "AQ==" },
            WebOptions);

        delete.Should().Contain("\"cascade\":true");
        delete.Should().Contain("\"rowVersion\":\"AQ==\"");

        var reorder = JsonSerializer.Serialize(
            new ReorderWbsNodesCommand
            {
                ParentChapterId = null,
                Nodes = [new WbsReorderNodeDto { NodeType = "CHAPTER", NodeId = Guid.NewGuid() }]
            },
            WebOptions);

        reorder.Should().Contain("\"parentChapterId\":null");
        reorder.Should().Contain("\"nodes\"");
        reorder.Should().Contain("\"nodeType\":\"CHAPTER\"");
    }

    [Fact]
    public void El_alta_de_item_no_acepta_apuId_y_expone_code_y_quantityWork()
    {
        // D-113 y SC-EDT-29.
        var json = JsonSerializer.Serialize(
            new CreateWbsItemCommand { Code = "CIM.1", WbsChapterId = null, QuantityWork = 12.5m },
            WebOptions);

        json.Should().Contain("\"code\":\"CIM.1\"");
        json.Should().Contain("\"wbsChapterId\":null");
        json.Should().Contain("\"quantityWork\":12.5");
        json.Should().NotContain("apuId");
    }

    [Fact]
    public void Los_codigos_de_error_publicados_coinciden_con_el_contrato()
    {
        // D-100 y D9: el middleware publica el código normalizado en `extensions.code`.
        var problemDetails = new Dictionary<string, object?>
        {
            ["status"] = 400,
            ["title"] = "Bad Request",
            ["type"] = "https://httpstatuses.com/400",
            ["detail"] = "El destino es el propio nodo o uno de sus descendientes.",
            ["instance"] = "/api/v1/projects/1/wbs/move",
            ["code"] = "WBS_MOVE_CYCLE",
            ["traceId"] = "0HN"
        };

        var json = JsonSerializer.Serialize(problemDetails, WebOptions);

        json.Should().Contain("\"code\":\"WBS_MOVE_CYCLE\"");
        json.Should().Contain("\"status\":400");
        json.Should().Contain("\"detail\"");
        json.Should().Contain("\"traceId\"");
    }

    private static WbsNodeDto CreateNode() => new()
    {
        Id = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        NodeType = "CHAPTER",
        Code = "CIM.1",
        ParentChapterId = null,
        Name = "Cimentación",
        QuantityWork = null,
        OrderIndex = 0,
        RowVersion = "AQ==",
        LastModifiedBy = Guid.NewGuid(),
        LastModifiedAt = DateTime.UtcNow,
        Children = []
    };
}
