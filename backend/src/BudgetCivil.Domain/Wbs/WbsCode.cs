namespace BudgetCivil.Domain.Wbs;

/// <summary>
/// Código manual y único del EDT por proyecto (`BR-015`, `D-30`, `D-51`, `D-117`). Capítulos e ítems
/// comparten esta tabla; el nodo apunta a su código con `CodeId` y no lo duplica (`D-31`).
/// </summary>
public sealed class WbsCode
{
    private WbsCode()
    {
    }

    public Guid Id { get; private set; }

    public Guid ProjectId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public WbsNodeType NodeType { get; private set; }

    public static WbsCode Create(Guid projectId, string? code, WbsNodeType nodeType)
    {
        if (projectId == Guid.Empty)
        {
            throw new InvalidOperationException("Project is required.");
        }

        return new WbsCode
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Code = WbsCodePolicy.NormalizeCode(code),
            NodeType = nodeType
        };
    }

    /// <summary>
    /// Cambio explícito del código: solo afecta a este registro y nunca reescribe a los descendientes
    /// (`SC-EDT-15`, `SC-EDT-19`, `D-117`).
    /// </summary>
    public void ChangeCode(string? code)
    {
        Code = WbsCodePolicy.NormalizeCode(code);
    }
}
