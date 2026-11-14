namespace BudgetCivil.Domain.Wbs;

/// <summary>
/// Conversión entre <see cref="WbsNodeType"/> y el valor persistido en `wbs_codes.node_type`
/// (`CHECK IN ('CHAPTER','ITEM')`, `PLAN §3.3.1`).
/// </summary>
public static class WbsNodeTypeCodes
{
    public const string Chapter = "CHAPTER";
    public const string Item = "ITEM";

    public static string ToCode(WbsNodeType nodeType) => nodeType switch
    {
        WbsNodeType.Chapter => Chapter,
        WbsNodeType.Item => Item,
        _ => throw new InvalidOperationException("WBS node type is invalid.")
    };

    public static bool TryParse(string? code, out WbsNodeType nodeType)
    {
        switch (code?.Trim().ToUpperInvariant())
        {
            case Chapter:
                nodeType = WbsNodeType.Chapter;
                return true;
            case Item:
                nodeType = WbsNodeType.Item;
                return true;
            default:
                nodeType = default;
                return false;
        }
    }

    public static WbsNodeType Parse(string? code)
    {
        if (!TryParse(code, out var nodeType))
        {
            throw new InvalidOperationException("WBS node type is invalid.");
        }

        return nodeType;
    }
}
