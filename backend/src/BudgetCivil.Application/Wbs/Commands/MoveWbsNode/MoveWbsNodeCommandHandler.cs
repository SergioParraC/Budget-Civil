using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Wbs.Dtos;
using BudgetCivil.Application.Wbs.Repositories;
using BudgetCivil.Application.Wbs.Services;
using BudgetCivil.Domain.Wbs;
using FluentValidation;

namespace BudgetCivil.Application.Wbs.Commands.MoveWbsNode;

public sealed class MoveWbsNodeCommand
{
    public string? NodeType { get; init; }

    public Guid NodeId { get; init; }

    public Guid? TargetParentChapterId { get; init; }

    public int? TargetIndex { get; init; }

    public string? RowVersion { get; init; }
}

public sealed class MoveWbsNodeCommandValidator : AbstractValidator<MoveWbsNodeCommand>
{
    public MoveWbsNodeCommandValidator()
    {
        RuleFor(command => command.NodeType)
            .NotEmpty();

        RuleFor(command => command.NodeId)
            .NotEqual(Guid.Empty);

        RuleFor(command => command.TargetParentChapterId)
            .NotEqual(Guid.Empty)
            .When(command => command.TargetParentChapterId.HasValue);

        RuleFor(command => command.TargetIndex)
            .GreaterThanOrEqualTo(0)
            .When(command => command.TargetIndex.HasValue);

        RuleFor(command => command.RowVersion)
            .NotEmpty();
    }
}

/// <summary>
/// Movimiento de un capítulo con su subárbol o de un ítem a otro capítulo del mismo proyecto o a la raíz
/// (`REQ-EDT-18`, `SC-EDT-62`…`70`, `SC-EDT-75`). El código nunca se recalcula.
/// </summary>
public sealed class MoveWbsNodeCommandHandler(
    WbsAccessService wbsAccessService,
    IWbsRepository wbsRepository,
    IValidator<MoveWbsNodeCommand> validator)
{
    public async Task<WbsNodeDto> ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        MoveWbsNodeCommand command,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);

        if (!WbsNodeTypeCodes.TryParse(command.NodeType, out var nodeType))
        {
            throw new ValidationAppException("nodeType es inválido.", WbsRules.ValidationError);
        }

        var requestedRowVersion = WbsRules.ParseRowVersion(command.RowVersion);
        await wbsAccessService.EnsureWriteAsync(actorUserId, projectId, cancellationToken);

        WbsChapter? chapter = null;
        WbsItem? item = null;
        Guid? sourceParentChapterId;
        byte[] currentRowVersion;

        if (nodeType == WbsNodeType.Chapter)
        {
            chapter = await wbsRepository.GetChapterAsync(projectId, command.NodeId, cancellationToken)
                ?? throw new NotFoundAppException("El capítulo no existe en el proyecto.", "WBS_NOT_FOUND");
            sourceParentChapterId = chapter.ParentChapterId;
            currentRowVersion = chapter.RowVersion;
        }
        else
        {
            item = await wbsRepository.GetItemAsync(projectId, command.NodeId, cancellationToken)
                ?? throw new NotFoundAppException("El ítem no existe en el proyecto.", "WBS_NOT_FOUND");
            sourceParentChapterId = item.WbsChapterId;
            currentRowVersion = item.RowVersion;
        }

        WbsRules.EnsureCurrentRowVersion(currentRowVersion, requestedRowVersion);

        var utcNow = DateTime.UtcNow;

        if (chapter is not null)
        {
            wbsRepository.SetOriginalRowVersion(chapter, requestedRowVersion);
            chapter.TouchAuthor(actorUserId, utcNow);
        }
        else
        {
            wbsRepository.SetOriginalRowVersion(item!, requestedRowVersion);
            item!.TouchAuthor(actorUserId, utcNow);
        }

        var targetChapterId = command.TargetParentChapterId;
        var targetExists = true;
        var targetIsChapter = true;
        var targetSameProject = true;

        if (targetChapterId.HasValue)
        {
            var targetChapter = await wbsRepository.GetChapterAsync(
                projectId,
                targetChapterId.Value,
                cancellationToken);

            if (targetChapter is null)
            {
                var targetItem = await wbsRepository.GetItemAsync(
                    projectId,
                    targetChapterId.Value,
                    cancellationToken);

                targetExists = targetItem is not null;
                targetIsChapter = false;
            }

            var parents = await wbsRepository.GetChapterParentsAsync(projectId, cancellationToken);
            var validation = WbsTreePolicy.ValidateMove(
                nodeType,
                command.NodeId,
                targetChapterId,
                targetExists,
                targetIsChapter,
                targetSameProject,
                parents);

            if (!validation.IsValid)
            {
                throw validation.ErrorCode switch
                {
                    WbsTreePolicy.TargetNotFound => new NotFoundAppException(
                        "El capítulo destino no existe en el proyecto.",
                        WbsTreePolicy.TargetNotFound),
                    WbsTreePolicy.MoveCycle => new ValidationAppException(
                        "El destino es el propio nodo o uno de sus descendientes.",
                        WbsTreePolicy.MoveCycle),
                    _ => new ValidationAppException(
                        "El destino debe ser un capítulo del mismo proyecto.",
                        WbsRules.ValidationError)
                };
            }
        }

        var siblings = await wbsRepository.GetChildrenAsync(projectId, targetChapterId, cancellationToken);
        var siblingsAfterRemoval = sourceParentChapterId == targetChapterId
            ? siblings.Where(sibling => sibling.NodeId != command.NodeId || sibling.NodeType != nodeType).ToList()
            : siblings.ToList();

        int targetIndex;
        try
        {
            targetIndex = WbsTreePolicy.ResolveTargetIndex(command.TargetIndex, siblingsAfterRemoval.Count);
        }
        catch (InvalidOperationException exception)
        {
            throw new ValidationAppException(exception.Message, WbsRules.ValidationError);
        }

        await wbsRepository.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            await wbsRepository.MoveNodeAsync(
                nodeType,
                command.NodeId,
                targetChapterId,
                targetIndex,
                transactionCancellationToken);
            await wbsRepository.SaveChangesAsync(transactionCancellationToken);

            if (sourceParentChapterId != targetChapterId)
            {
                await wbsRepository.CompactAsync(projectId, sourceParentChapterId, transactionCancellationToken);
            }

            await wbsRepository.UpsertActivityAsync(
                projectId,
                actorUserId,
                utcNow,
                transactionCancellationToken);
            await wbsRepository.SaveChangesAsync(transactionCancellationToken);
        }, cancellationToken);

        return nodeType == WbsNodeType.Chapter
            ? WbsMapper.ToChapterNode(chapter!)
            : WbsMapper.ToItemNode(item!);
    }

    private async Task ValidateAsync(MoveWbsNodeCommand command, CancellationToken cancellationToken)
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
