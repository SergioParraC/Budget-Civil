using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Wbs.Dtos;
using BudgetCivil.Application.Wbs.Repositories;
using BudgetCivil.Application.Wbs.Services;
using BudgetCivil.Domain.Wbs;
using FluentValidation;

namespace BudgetCivil.Application.Wbs.Commands.UpdateWbsChapter;

public sealed class UpdateWbsChapterCommand
{
    public string? Name { get; init; }

    public string? Code { get; init; }

    public string? RowVersion { get; init; }
}

public sealed class UpdateWbsChapterCommandValidator : AbstractValidator<UpdateWbsChapterCommand>
{
    public UpdateWbsChapterCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(WbsChapter.NameMaxLength);

        RuleFor(command => command.RowVersion)
            .NotEmpty();
    }
}

/// <summary>
/// Edición explícita del nombre y del código; no cambia el padre ni reescribe a los descendientes
/// (`SC-EDT-15`, `SC-EDT-19`, `SC-EDT-37`, `SC-EDT-38`).
/// </summary>
public sealed class UpdateWbsChapterCommandHandler(
    WbsAccessService wbsAccessService,
    IWbsRepository wbsRepository,
    IValidator<UpdateWbsChapterCommand> validator)
{
    public async Task<WbsNodeDto> ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        Guid chapterId,
        UpdateWbsChapterCommand command,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        var code = WbsRules.EnsureValidCode(command.Code);
        var name = WbsRules.EnsureValidName(command.Name);
        var requestedRowVersion = WbsRules.ParseRowVersion(command.RowVersion);

        await wbsAccessService.EnsureWriteAsync(actorUserId, projectId, cancellationToken);

        var chapter = await wbsRepository.GetChapterAsync(projectId, chapterId, cancellationToken)
            ?? throw new NotFoundAppException("El capítulo no existe en el proyecto.", "WBS_NOT_FOUND");

        WbsRules.EnsureCurrentRowVersion(chapter.RowVersion, requestedRowVersion);
        wbsRepository.SetOriginalRowVersion(chapter, requestedRowVersion);

        if (await wbsRepository.CodeExistsAsync(projectId, code, chapter.CodeId, cancellationToken))
        {
            throw new ConflictAppException(
                "El código ya existe en el proyecto.",
                WbsRules.CodeDuplicated);
        }

        var utcNow = DateTime.UtcNow;
        chapter.Rename(name);
        chapter.Code.ChangeCode(code);
        chapter.TouchAuthor(actorUserId, utcNow);

        await wbsRepository.UpsertActivityAsync(projectId, actorUserId, utcNow, cancellationToken);
        await wbsRepository.SaveChangesAsync(cancellationToken);

        return WbsMapper.ToChapterNode(chapter);
    }

    private async Task ValidateAsync(
        UpdateWbsChapterCommand command,
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
