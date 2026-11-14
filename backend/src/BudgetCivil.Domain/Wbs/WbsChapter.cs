namespace BudgetCivil.Domain.Wbs;

/// <summary>
/// Capítulo o subcapítulo del EDT (`RF-04`, `REQ-EDT-01`, `REQ-EDT-02`). La jerarquía se determina por
/// `ParentChapterId`; el código es un identificador libre que nunca se recompone (`D-30`, `D-117`).
/// </summary>
public sealed class WbsChapter
{
    public const int NameMaxLength = 255;

    private WbsChapter()
    {
    }

    public Guid Id { get; private set; }

    public Guid ProjectId { get; private set; }

    public Guid? ParentChapterId { get; private set; }

    public Guid CodeId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public int OrderIndex { get; private set; }

    public Guid LastModifiedByUserId { get; private set; }

    public DateTime LastModifiedAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public WbsCode Code { get; private set; } = null!;

    public static WbsChapter Create(
        Guid projectId,
        Guid? parentChapterId,
        WbsCode code,
        string? name,
        int orderIndex,
        Guid actorUserId,
        DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(code);

        if (projectId == Guid.Empty)
        {
            throw new InvalidOperationException("Project is required.");
        }

        if (code.ProjectId != projectId || code.NodeType != WbsNodeType.Chapter)
        {
            throw new InvalidOperationException("WBS chapter code is invalid.");
        }

        if (parentChapterId == Guid.Empty)
        {
            throw new InvalidOperationException("WBS parent chapter is invalid.");
        }

        if (orderIndex < 0)
        {
            throw new InvalidOperationException("WBS order index is invalid.");
        }

        if (actorUserId == Guid.Empty)
        {
            throw new InvalidOperationException("WBS author is required.");
        }

        var chapter = new WbsChapter
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            ParentChapterId = parentChapterId,
            CodeId = code.Id,
            Code = code,
            OrderIndex = orderIndex,
            LastModifiedByUserId = actorUserId,
            LastModifiedAtUtc = utcNow
        };

        chapter.Name = NormalizeName(name);
        return chapter;
    }

    /// <summary>Cambia solo el nombre; el código y la jerarquía permanecen (`REQ-EDT-05`).</summary>
    public void Rename(string? name)
    {
        Name = NormalizeName(name);
        TouchAuthor(LastModifiedByUserId, DateTime.UtcNow);
    }

    /// <summary>Cambio explícito del código del capítulo; no reescribe a los descendientes (`SC-EDT-19`).</summary>
    public void ChangeCode(WbsCode code)
    {
        ArgumentNullException.ThrowIfNull(code);

        if (code.ProjectId != ProjectId || code.NodeType != WbsNodeType.Chapter)
        {
            throw new InvalidOperationException("WBS chapter code is invalid.");
        }

        CodeId = code.Id;
        Code = code;
    }

    /// <summary>Cambia el padre y la posición sin tocar el código ni el subárbol (`REQ-EDT-18`).</summary>
    public void MoveTo(Guid? parentChapterId, int orderIndex)
    {
        if (parentChapterId == Guid.Empty)
        {
            throw new InvalidOperationException("WBS parent chapter is invalid.");
        }

        if (parentChapterId == Id)
        {
            throw new InvalidOperationException("WBS move cycle.");
        }

        if (orderIndex < 0)
        {
            throw new InvalidOperationException("WBS order index is invalid.");
        }

        ParentChapterId = parentChapterId;
        OrderIndex = orderIndex;
    }

    public void SetOrderIndex(int orderIndex)
    {
        if (orderIndex < 0)
        {
            throw new InvalidOperationException("WBS order index is invalid.");
        }

        OrderIndex = orderIndex;
    }

    /// <summary>Registra al actor autenticado y la fecha UTC de la modificación (`D-116`).</summary>
    public void TouchAuthor(Guid actorUserId, DateTime utcNow)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new InvalidOperationException("WBS author is required.");
        }

        LastModifiedByUserId = actorUserId;
        LastModifiedAtUtc = utcNow;
    }

    public static string NormalizeName(string? name)
    {
        var normalized = name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > NameMaxLength)
        {
            throw new InvalidOperationException("WBS chapter name is invalid.");
        }

        return normalized;
    }
}
