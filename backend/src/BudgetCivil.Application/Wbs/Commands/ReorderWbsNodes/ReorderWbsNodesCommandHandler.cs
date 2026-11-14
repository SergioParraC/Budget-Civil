using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Wbs.Dtos;
using BudgetCivil.Application.Wbs.Repositories;
using BudgetCivil.Application.Wbs.Services;
using BudgetCivil.Domain.Wbs;
using FluentValidation;

namespace BudgetCivil.Application.Wbs.Commands.ReorderWbsNodes;

public sealed class ReorderWbsNodesCommand
{
    public Guid? ParentChapterId { get; init; }

    public IReadOnlyList<WbsReorderNodeDto> Nodes { get; init; } = [];
}

public sealed class ReorderWbsNodesCommandValidator : AbstractValidator<ReorderWbsNodesCommand>
{
    public ReorderWbsNodesCommandValidator()
    {
        RuleFor(command => command.ParentChapterId)
            .NotEqual(Guid.Empty)
            .When(command => command.ParentChapterId.HasValue);

        RuleFor(command => command.Nodes)
            .NotEmpty();

        RuleForEach(command => command.Nodes).ChildRules(node =>
        {
            node.RuleFor(entry => entry.NodeType).NotEmpty();
            node.RuleFor(entry => entry.NodeId).NotEqual(Guid.Empty);
        });
    }
}

/// <summary>
/// Reordenamiento de los hijos de un padre, incluida la raíz (`SC-EDT-34`, `SC-EDT-36`). La lista debe
/// contener exactamente los hijos vigentes; solo cambian las posiciones, nunca los códigos (`D-117`).
/// </summary>
public sealed class ReorderWbsNodesCommandHandler(
    WbsAccessService wbsAccessService,
    IWbsRepository wbsRepository,
    IValidator<ReorderWbsNodesCommand> validator)
{
    public async Task<ReorderWbsNodesResponse> ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        ReorderWbsNodesCommand command,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        await wbsAccessService.EnsureWriteAsync(actorUserId, projectId, cancellationToken);

        if (command.ParentChapterId.HasValue)
        {
            var parent = await wbsRepository.GetChapterAsync(
                projectId,
                command.ParentChapterId.Value,
                cancellationToken);

            if (parent is null)
            {
                throw new NotFoundAppException(
                    "El capítulo padre no existe en el proyecto.",
                    WbsTreePolicy.TargetNotFound);
            }
        }

        var currentChildren = await wbsRepository.GetChildrenAsync(
            projectId,
            command.ParentChapterId,
            cancellationToken);

        var requestedNodes = new List<WbsNodeRef>(command.Nodes.Count);
        foreach (var node in command.Nodes)
        {
            if (!WbsNodeTypeCodes.TryParse(node.NodeType, out var nodeType))
            {
                throw new ValidationAppException("nodeType es inválido.", WbsRules.ValidationError);
            }

            requestedNodes.Add(new WbsNodeRef(nodeType, node.NodeId, 0));
        }

        if (requestedNodes.Count != currentChildren.Count ||
            requestedNodes.DistinctBy(node => (node.NodeType, node.NodeId)).Count() != requestedNodes.Count ||
            requestedNodes.Any(requested => !currentChildren.Any(current =>
                current.NodeType == requested.NodeType && current.NodeId == requested.NodeId)))
        {
            throw new ValidationAppException(
                "La secuencia debe contener exactamente los hijos vigentes del padre.",
                WbsRules.ValidationError);
        }

        var orderedNodes = requestedNodes
            .Select((node, index) => node with { OrderIndex = index })
            .ToList();

        var utcNow = DateTime.UtcNow;

        await wbsRepository.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            await wbsRepository.ReorderAsync(
                projectId,
                command.ParentChapterId,
                orderedNodes,
                transactionCancellationToken);
            await wbsRepository.UpsertActivityAsync(
                projectId,
                actorUserId,
                utcNow,
                transactionCancellationToken);
            await wbsRepository.SaveChangesAsync(transactionCancellationToken);
        }, cancellationToken);

        return new ReorderWbsNodesResponse
        {
            ParentChapterId = command.ParentChapterId,
            Nodes = orderedNodes
                .Select(node => new WbsNodeRefDto
                {
                    NodeType = WbsNodeTypeCodes.ToCode(node.NodeType),
                    NodeId = node.NodeId,
                    OrderIndex = node.OrderIndex
                })
                .ToList()
        };
    }

    private async Task ValidateAsync(ReorderWbsNodesCommand command, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(command, cancellationToken);
        if (result.IsValid)
        {
            return;
        }

        var message = string.Join(" | ", result.Errors.Select(error => error.ErrorMessage));
        throw new ValidationAppException(message, WbsRules.ValidationError);
    }
}
