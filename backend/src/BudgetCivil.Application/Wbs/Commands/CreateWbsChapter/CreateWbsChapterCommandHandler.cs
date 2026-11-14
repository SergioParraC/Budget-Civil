using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Wbs.Dtos;
using BudgetCivil.Application.Wbs.Repositories;
using BudgetCivil.Application.Wbs.Services;
using BudgetCivil.Domain.Wbs;
using FluentValidation;

namespace BudgetCivil.Application.Wbs.Commands.CreateWbsChapter;

public sealed class CreateWbsChapterCommand
{
    public string? Code { get; init; }

    public string? Name { get; init; }

    public Guid? ParentChapterId { get; init; }
}

public sealed class CreateWbsChapterCommandValidator : AbstractValidator<CreateWbsChapterCommand>
{
    public CreateWbsChapterCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(WbsChapter.NameMaxLength);

        RuleFor(command => command.ParentChapterId)
            .NotEqual(Guid.Empty)
            .When(command => command.ParentChapterId.HasValue);
    }
}

/// <summary>
/// Alta de capítulo con código manual obligatorio (`REQ-EDT-03`, `SC-EDT-01`, `SC-EDT-09`).
/// </summary>
public sealed class CreateWbsChapterCommandHandler(
    WbsAccessService wbsAccessService,
    IWbsRepository wbsRepository,
    IValidator<CreateWbsChapterCommand> validator)
{
    public async Task<WbsNodeDto> ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        CreateWbsChapterCommand command,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        var code = WbsRules.EnsureValidCode(command.Code);
        var name = WbsRules.EnsureValidName(command.Name);

        await wbsAccessService.EnsureWriteAsync(actorUserId, projectId, cancellationToken);

        if (command.ParentChapterId.HasValue)
        {
            var parent = await wbsRepository.GetChapterAsync(
                projectId,
                command.ParentChapterId.Value,
                cancellationToken);

            if (parent is null)
            {
                throw new ValidationAppException(
                    "El capítulo padre no existe en el proyecto.",
                    WbsRules.ValidationError);
            }
        }

        if (await wbsRepository.CodeExistsAsync(projectId, code, null, cancellationToken))
        {
            throw new ConflictAppException(
                "El código ya existe en el proyecto.",
                WbsRules.CodeDuplicated);
        }

        var utcNow = DateTime.UtcNow;
        var orderIndex = await wbsRepository.CountChildrenAsync(
            projectId,
            command.ParentChapterId,
            cancellationToken);

        WbsChapter chapter;
        try
        {
            var wbsCode = WbsCode.Create(projectId, code, WbsNodeType.Chapter);
            chapter = WbsChapter.Create(
                projectId,
                command.ParentChapterId,
                wbsCode,
                name,
                orderIndex,
                actorUserId,
                utcNow);
        }
        catch (InvalidOperationException exception)
        {
            throw new ValidationAppException(exception.Message, WbsRules.ValidationError);
        }

        await wbsRepository.AddChapterAsync(chapter, cancellationToken);
        await wbsRepository.UpsertActivityAsync(projectId, actorUserId, utcNow, cancellationToken);
        await wbsRepository.SaveChangesAsync(cancellationToken);

        return WbsMapper.ToChapterNode(chapter);
    }

    private async Task ValidateAsync(
        CreateWbsChapterCommand command,
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
