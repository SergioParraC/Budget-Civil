namespace BudgetCivil.Application.Calculation;

/// <summary>Capítulo del EDT reducido a su lugar en la jerarquía (`D2`, `REQ-EDT-23`).</summary>
public sealed record ChapterCostNode(Guid ChapterId, Guid? ParentChapterId);

/// <summary>Ítem del EDT con su costo directo derivado; nulo cuando su APU está incompleto.</summary>
public sealed record ChapterCostItem(Guid WbsChapterId, decimal? DirectCost);

/// <summary>
/// Agregación B del capítulo (`D2`): acumula los costos de los ítems del subárbol una sola vez, sin tomar
/// el subtotal publicado de un capítulo hijo como si fuera un ítem adicional. Un capítulo sin ítems en su
/// subárbol devuelve `0`; si algún ítem del subárbol es nulo, el capítulo devuelve `null`.
/// </summary>
public static class WbsCostAggregation
{
    public static IReadOnlyDictionary<Guid, decimal?> AggregateChapterCosts(
        IReadOnlyCollection<ChapterCostNode> chapters,
        IReadOnlyCollection<ChapterCostItem> items,
        int roundingDecimals)
    {
        ArgumentNullException.ThrowIfNull(chapters);
        ArgumentNullException.ThrowIfNull(items);

        var chaptersByParent = chapters.ToLookup(chapter => chapter.ParentChapterId);
        var itemsByChapter = items.ToLookup(item => item.WbsChapterId);
        var results = new Dictionary<Guid, decimal?>();
        var visiting = new HashSet<Guid>();

        decimal? AccumulateItems(Guid chapterId)
        {
            if (results.TryGetValue(chapterId, out var cached))
            {
                return cached;
            }

            if (!visiting.Add(chapterId))
            {
                // Un ciclo no es un árbol válido; se corta sin acumular dos veces.
                return 0m;
            }

            decimal? total = 0m;

            foreach (var item in itemsByChapter[chapterId])
            {
                if (!item.DirectCost.HasValue)
                {
                    total = null;
                    break;
                }

                total += item.DirectCost.Value;
            }

            if (total.HasValue)
            {
                foreach (var child in chaptersByParent[chapterId])
                {
                    var subtotal = AccumulateItems(child.ChapterId);
                    if (!subtotal.HasValue)
                    {
                        total = null;
                        break;
                    }

                    total += subtotal.Value;
                }
            }

            visiting.Remove(chapterId);

            var rounded = total.HasValue
                ? ApuCostCalculator.Round(total.Value, roundingDecimals)
                : (decimal?)null;

            results[chapterId] = rounded;
            return rounded;
        }

        foreach (var chapter in chapters)
        {
            AccumulateItems(chapter.ChapterId);
        }

        return results;
    }
}
