namespace BudgetCivil.Domain.Wbs;

/// <summary>
/// Ítem de obra del EDT (`RF-04`, `RF-22`, `REQ-EDT-07`). `WbsChapterId` nulo representa un ítem raíz
/// (`D-118`); `ApuId` queda nulo porque el APU llega en `I-3b` (`D-113`).
/// </summary>
public sealed class WbsItem
{
    private WbsItem()
    {
    }

    public Guid Id { get; private set; }

    public Guid ProjectId { get; private set; }

    public Guid? WbsChapterId { get; private set; }

    public Guid? ApuId { get; private set; }

    public Guid CodeId { get; private set; }

    public decimal QuantityWork { get; private set; }

    public int OrderIndex { get; private set; }

    public Guid LastModifiedByUserId { get; private set; }

    public DateTime LastModifiedAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public WbsCode Code { get; private set; } = null!;

    public static WbsItem Create(
        Guid projectId,
        Guid? wbsChapterId,
        WbsCode code,
        decimal quantityWork,
        int orderIndex,
        Guid actorUserId,
        DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(code);

        if (projectId == Guid.Empty)
        {
            throw new InvalidOperationException("Project is required.");
        }

        if (code.ProjectId != projectId || code.NodeType != WbsNodeType.Item)
        {
            throw new InvalidOperationException("WBS item code is invalid.");
        }

        if (wbsChapterId == Guid.Empty)
        {
            throw new InvalidOperationException("WBS parent chapter is invalid.");
        }

        if (quantityWork <= 0m)
        {
            throw new InvalidOperationException("WBS quantity work is invalid.");
        }

        if (orderIndex < 0)
        {
            throw new InvalidOperationException("WBS order index is invalid.");
        }

        if (actorUserId == Guid.Empty)
        {
            throw new InvalidOperationException("WBS author is required.");
        }

        return new WbsItem
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            WbsChapterId = wbsChapterId,
            ApuId = null,
            CodeId = code.Id,
            Code = code,
            QuantityWork = quantityWork,
            OrderIndex = orderIndex,
            LastModifiedByUserId = actorUserId,
            LastModifiedAtUtc = utcNow
        };
    }

    /// <summary>Cambia la cantidad de obra; debe ser mayor que cero (`SC-EDT-25`…`27`).</summary>
    public void ChangeQuantity(decimal quantityWork)
    {
        if (quantityWork <= 0m)
        {
            throw new InvalidOperationException("WBS quantity work is invalid.");
        }

        QuantityWork = quantityWork;
    }

    /// <summary>Cambio explícito del código del ítem; no toca ningún otro nodo (`D-117`).</summary>
    public void ChangeCode(WbsCode code)
    {
        ArgumentNullException.ThrowIfNull(code);

        if (code.ProjectId != ProjectId || code.NodeType != WbsNodeType.Item)
        {
            throw new InvalidOperationException("WBS item code is invalid.");
        }

        CodeId = code.Id;
        Code = code;
    }

    /// <summary>Cambia el capítulo padre (o la raíz) y la posición; el código y la cantidad no cambian.</summary>
    public void MoveTo(Guid? wbsChapterId, int orderIndex)
    {
        if (wbsChapterId == Guid.Empty)
        {
            throw new InvalidOperationException("WBS parent chapter is invalid.");
        }

        if (orderIndex < 0)
        {
            throw new InvalidOperationException("WBS order index is invalid.");
        }

        WbsChapterId = wbsChapterId;
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

    /// <summary>El APU no se acepta en este incremento (`D-113`, `SC-EDT-28`, `SC-EDT-29`).</summary>
    public void IgnoreApuReference()
    {
        ApuId = null;
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
}
