namespace BudgetCivil.Domain.Wbs;

public sealed record WbsMoveValidation(bool IsValid, string? ErrorCode);

/// <summary>
/// Reglas puras de movimiento, posición y borrado del árbol (`REQ-EDT-10`, `REQ-EDT-15`, `REQ-EDT-18`,
/// `D-118`, `D-119`). No conoce EF ni ASP.NET.
/// </summary>
public static class WbsTreePolicy
{
    public const string TargetNotFound = "WBS_TARGET_NOT_FOUND";
    public const string MoveCycle = "WBS_MOVE_CYCLE";
    public const string ValidationError = "VALIDATION_ERROR";

    /// <summary>
    /// Valida el destino de un movimiento antes de tocar la base: capítulo existente del mismo proyecto,
    /// destino distinto del propio nodo y de sus descendientes, y `targetIndex` dentro de la lista restante.
    /// </summary>
    public static WbsMoveValidation ValidateMove(
        WbsNodeType nodeType,
        Guid nodeId,
        Guid? targetChapterId,
        bool targetExists,
        bool targetIsChapter,
        bool targetSameProject,
        IReadOnlyDictionary<Guid, Guid?> parentChapterIdByChapter)
    {
        if (!targetChapterId.HasValue)
        {
            return new WbsMoveValidation(true, null);
        }

        if (!targetExists || !targetSameProject)
        {
            return new WbsMoveValidation(false, TargetNotFound);
        }

        if (!targetIsChapter)
        {
            return new WbsMoveValidation(false, ValidationError);
        }

        if (nodeType == WbsNodeType.Chapter &&
            IsChapterDescendant(targetChapterId.Value, nodeId, parentChapterIdByChapter))
        {
            return new WbsMoveValidation(false, MoveCycle);
        }

        return new WbsMoveValidation(true, null);
    }

    /// <summary>
    /// Indica si <paramref name="candidateChapterId"/> está dentro del subárbol de
    /// <paramref name="ancestorChapterId"/>, incluido él mismo.
    /// </summary>
    public static bool IsChapterDescendant(
        Guid candidateChapterId,
        Guid ancestorChapterId,
        IReadOnlyDictionary<Guid, Guid?> parentChapterIdByChapter)
    {
        var current = candidateChapterId;
        var visited = new HashSet<Guid>();

        while (visited.Add(current))
        {
            if (current == ancestorChapterId)
            {
                return true;
            }

            if (!parentChapterIdByChapter.TryGetValue(current, out var parentChapterId) ||
                !parentChapterId.HasValue)
            {
                return false;
            }

            current = parentChapterId.Value;
        }

        return false;
    }

    /// <summary>
    /// El índice se interpreta después de retirar el nodo de su padre actual: `null` significa al final y
    /// solo se acepta entre `0` y la cantidad de hermanos restantes (`SC-EDT-70`, `SC-EDT-75`).
    /// </summary>
    public static int ResolveTargetIndex(int? targetIndex, int siblingCountAfterRemoval)
    {
        if (siblingCountAfterRemoval < 0)
        {
            throw new InvalidOperationException(ValidationError);
        }

        if (!targetIndex.HasValue)
        {
            return siblingCountAfterRemoval;
        }

        if (targetIndex.Value < 0 || targetIndex.Value > siblingCountAfterRemoval)
        {
            throw new InvalidOperationException(ValidationError);
        }

        return targetIndex.Value;
    }

    /// <summary>
    /// Orden de borrado de los capítulos de un subárbol: primero los de mayor profundidad y al final la
    /// raíz, para que `RESTRICT` nunca impida la cascada (`D-119`).
    /// </summary>
    public static IReadOnlyList<WbsChapter> OrderChaptersForDeletion(IReadOnlyCollection<WbsChapter> subtree)
    {
        var parents = subtree.ToDictionary(chapter => chapter.Id, chapter => chapter.ParentChapterId);

        return subtree
            .OrderByDescending(chapter => GetDepth(chapter.Id, parents))
            .ThenByDescending(chapter => chapter.Id)
            .ToList();
    }

    /// <summary>Profundidad relativa dentro del subárbol; la raíz del subárbol queda en cero.</summary>
    public static int GetDepth(Guid chapterId, IReadOnlyDictionary<Guid, Guid?> parentChapterIdByChapter)
    {
        var depth = 0;
        var current = chapterId;
        var visited = new HashSet<Guid>();

        while (visited.Add(current) &&
               parentChapterIdByChapter.TryGetValue(current, out var parentChapterId) &&
               parentChapterId.HasValue)
        {
            depth++;
            current = parentChapterId.Value;
        }

        return depth;
    }
}
