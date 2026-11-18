using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Wbs.Repositories;
using BudgetCivil.Application.Wbs.Services;
using FluentValidation;

namespace BudgetCivil.Application.Wbs.Commands.DeleteWbsChapter;

public sealed class DeleteWbsChapterCommand
{
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
/// Baja física del capítulo solo cuando está vacío (`REQ-EDT-21`, `SC-EDT-82`…`84`, `D-114`, `D-119`).
/// Con APUs en el subárbol responde `409 WBS_CHAPTER_HAS_APU`; con subcapítulos o ítems responde
/// `409 WBS_NODE_HAS_CHILDREN`. No existe baja en cascada.
/// </summary>
public sealed class DeleteWbsChapterCommandHandler(
    WbsAccessService wbsAccessService,
    IWbsRepository wbsRepository,
    IWbsItemApuCleaner wbsItemApuCleaner,
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

        if (await wbsItemApuCleaner.HasApusInSubtreeAsync(projectId, chapterId, cancellationToken))
        {
            throw new ConflictAppException(
                "El capítulo contiene APUs; elimina primero los ítems.",
                "WBS_CHAPTER_HAS_APU");
        }

        if (subtree.Count > 1 || items.Count > 0)
        {
            throw new ConflictAppException(
                "El capítulo tiene descendientes; solo se elimina un capítulo vacío.",
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
