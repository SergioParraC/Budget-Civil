using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Wbs.Repositories;
using BudgetCivil.Application.Wbs.Services;
using FluentValidation;

namespace BudgetCivil.Application.Wbs.Commands.DeleteWbsChapter;

public sealed class DeleteWbsChapterCommand
{
    public bool Cascade { get; init; }

    public string? RowVersion { get; init; }
}

public sealed class DeleteWbsChapterCommandValidator : AbstractValidator<DeleteWbsChapterCommand>
{
    public DeleteWbsChapterCommandValidator()
    {
        RuleFor(command => command.RowVersion)
            .NotEmpty();
    }
}

/// <summary>
/// Baja física con cascada confirmada (`REQ-EDT-15`, `SC-EDT-52`…`54`, `D-119`). Sin confirmación, un
/// capítulo con descendientes responde `409 WBS_NODE_HAS_CHILDREN`; la base conserva `RESTRICT`.
/// </summary>
public sealed class DeleteWbsChapterCommandHandler(
    WbsAccessService wbsAccessService,
    IWbsRepository wbsRepository,
    IValidator<DeleteWbsChapterCommand> validator)
{
    public async Task ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        Guid chapterId,
        DeleteWbsChapterCommand command,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        var requestedRowVersion = WbsRules.ParseRowVersion(command.RowVersion);

        await wbsAccessService.EnsureWriteAsync(actorUserId, projectId, cancellationToken);

        var chapter = await wbsRepository.GetChapterAsync(projectId, chapterId, cancellationToken)
            ?? throw new NotFoundAppException("El capítulo no existe en el proyecto.", "WBS_NOT_FOUND");

        WbsRules.EnsureCurrentRowVersion(chapter.RowVersion, requestedRowVersion);
        wbsRepository.SetOriginalRowVersion(chapter, requestedRowVersion);

        var subtree = await wbsRepository.GetSubtreeChaptersAsync(projectId, chapterId, cancellationToken);

        var items = await wbsRepository.GetItemsByChapterIdsAsync(
            projectId,
            subtree.Select(subtreeChapter => subtreeChapter.Id).ToList(),
            cancellationToken);

        if ((subtree.Count > 1 || items.Count > 0) && !command.Cascade)
        {
            throw new ConflictAppException(
                "El capítulo tiene descendientes; confirma la cascada para eliminarlo.",
                "WBS_NODE_HAS_CHILDREN");
        }

        var sourceParentChapterId = chapter.ParentChapterId;
        var utcNow = DateTime.UtcNow;

        await wbsRepository.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            await wbsRepository.DeleteSubtreeAsync(subtree, items, transactionCancellationToken);
            await wbsRepository.SaveChangesAsync(transactionCancellationToken);

            await wbsRepository.CompactAsync(projectId, sourceParentChapterId, transactionCancellationToken);
            await wbsRepository.UpsertActivityAsync(
                projectId,
                actorUserId,
                utcNow,
                transactionCancellationToken);
            await wbsRepository.SaveChangesAsync(transactionCancellationToken);
        }, cancellationToken);
    }

    private async Task ValidateAsync(
        DeleteWbsChapterCommand command,
        CancellationToken cancellationToken)
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
