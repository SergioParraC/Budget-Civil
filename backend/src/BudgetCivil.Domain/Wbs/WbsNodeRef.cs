namespace BudgetCivil.Domain.Wbs;

/// <summary>
/// Referencia mínima a un nodo dentro de la secuencia de hermanos de un padre, incluida la raíz.
/// </summary>
public sealed record WbsNodeRef(WbsNodeType NodeType, Guid NodeId, int OrderIndex);
