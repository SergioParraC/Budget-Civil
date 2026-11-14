using BudgetCivil.Application.Wbs.Repositories;
using BudgetCivil.Domain.Wbs;

namespace BudgetCivil.Application.Wbs.Dtos;

/// <summary>
/// Nodo del árbol v1 (`D-41`): nunca se exponen entidades; los ítems raíz comparten la colección raíz
/// con los capítulos (`D-118`).
/// </summary>
public sealed class WbsNodeDto
{
    public Guid Id { get; init; }

    public Guid ProjectId { get; init; }

    public string NodeType { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public Guid? ParentChapterId { get; init; }

    public string? Name { get; init; }

    public decimal? QuantityWork { get; init; }

    public int OrderIndex { get; init; }

    public string RowVersion { get; init; } = string.Empty;

    public Guid LastModifiedBy { get; init; }

    public DateTime LastModifiedAt { get; init; }

    public IReadOnlyList<WbsNodeDto> Children { get; init; } = [];
}

public sealed class GetWbsTreeResponse
{
    public Guid ProjectId { get; init; }

    public IReadOnlyList<WbsNodeDto> Nodes { get; init; } = [];
}

public sealed class WbsSearchItemDto
{
    public Guid NodeId { get; init; }

    public string NodeType { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public string? Name { get; init; }

    public decimal? QuantityWork { get; init; }

    public Guid? ParentChapterId { get; init; }

    public int OrderIndex { get; init; }
}

public sealed class SearchWbsResponse
{
    public IReadOnlyList<WbsSearchItemDto> Items { get; init; } = [];

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalItems { get; init; }
}

public sealed class ValidateWbsCodeResponse
{
    public string Code { get; init; } = string.Empty;

    public bool IsValid { get; init; }

    public bool IsAvailable { get; init; }

    public string? Reason { get; init; }
}

public sealed class WbsActivityParticipantDto
{
    public Guid UserId { get; init; }

    public string UserName { get; init; } = string.Empty;

    public DateTime LastModifiedAt { get; init; }
}

public sealed class GetWbsActivityResponse
{
    public Guid ProjectId { get; init; }

    public WbsActivityParticipantDto? LastActivity { get; init; }

    public IReadOnlyList<WbsActivityParticipantDto> Participants { get; init; } = [];
}

public sealed class WbsReorderNodeDto
{
    public string NodeType { get; init; } = string.Empty;

    public Guid NodeId { get; init; }
}

public sealed class WbsNodeRefDto
{
    public string NodeType { get; init; } = string.Empty;

    public Guid NodeId { get; init; }

    public int OrderIndex { get; init; }
}

public sealed class ReorderWbsNodesResponse
{
    public Guid? ParentChapterId { get; init; }

    public IReadOnlyList<WbsNodeRefDto> Nodes { get; init; } = [];
}

public static class WbsMapper
{
    public static WbsNodeDto ToChapterNode(
        WbsChapter chapter,
        IReadOnlyList<WbsNodeDto>? children = null) => new()
    {
        Id = chapter.Id,
        ProjectId = chapter.ProjectId,
        NodeType = WbsNodeTypeCodes.Chapter,
        Code = chapter.Code.Code,
        ParentChapterId = chapter.ParentChapterId,
        Name = chapter.Name,
        QuantityWork = null,
        OrderIndex = chapter.OrderIndex,
        RowVersion = Convert.ToBase64String(chapter.RowVersion),
        LastModifiedBy = chapter.LastModifiedByUserId,
        LastModifiedAt = chapter.LastModifiedAtUtc,
        Children = children ?? []
    };

    public static WbsNodeDto ToItemNode(WbsItem item) => new()
    {
        Id = item.Id,
        ProjectId = item.ProjectId,
        NodeType = WbsNodeTypeCodes.Item,
        Code = item.Code.Code,
        ParentChapterId = item.WbsChapterId,
        Name = null,
        QuantityWork = item.QuantityWork,
        OrderIndex = item.OrderIndex,
        RowVersion = Convert.ToBase64String(item.RowVersion),
        LastModifiedBy = item.LastModifiedByUserId,
        LastModifiedAt = item.LastModifiedAtUtc,
        Children = []
    };

    public static WbsSearchItemDto ToSearchItem(WbsSearchItem item) => new()
    {
        NodeId = item.NodeId,
        NodeType = WbsNodeTypeCodes.ToCode(item.NodeType),
        Code = item.Code,
        Name = item.Name,
        QuantityWork = item.QuantityWork,
        ParentChapterId = item.ParentChapterId,
        OrderIndex = item.OrderIndex
    };

    public static GetWbsActivityResponse ToActivityResponse(
        Guid projectId,
        WbsActivitySnapshot snapshot) => new()
    {
        ProjectId = projectId,
        LastActivity = snapshot.LastActivity is null ? null : ToParticipant(snapshot.LastActivity),
        Participants = snapshot.Participants.Select(ToParticipant).ToList()
    };

    public static WbsActivityParticipantDto ToParticipant(WbsActivityEntry entry) => new()
    {
        UserId = entry.UserId,
        UserName = entry.UserName,
        LastModifiedAt = entry.LastModifiedAtUtc
    };
}
