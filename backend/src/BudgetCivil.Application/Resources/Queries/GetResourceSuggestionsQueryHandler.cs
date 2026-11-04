using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Resources.Dtos;
using BudgetCivil.Application.Resources.Repositories;
using BudgetCivil.Application.Resources.Services;
using BudgetCivil.Domain.Resources;
using FluentValidation;

namespace BudgetCivil.Application.Resources.Queries;

public sealed class GetResourceSuggestionsQuery
{
    public string? Description { get; init; }

    public string? UserKey { get; init; }
}

public sealed class GetResourceSuggestionsQueryValidator : AbstractValidator<GetResourceSuggestionsQuery>
{
    public GetResourceSuggestionsQueryValidator()
    {
        RuleFor(query => query.Description)
            .MaximumLength(Resource.DescriptionMaxLength)
            .When(query => !string.IsNullOrWhiteSpace(query.Description));

        RuleFor(query => query.UserKey)
            .MaximumLength(Resource.UserKeyMaxLength)
            .When(query => !string.IsNullOrWhiteSpace(query.UserKey));

        RuleFor(query => query)
            .Must(query =>
                !string.IsNullOrWhiteSpace(query.Description) ||
                !string.IsNullOrWhiteSpace(query.UserKey))
            .WithMessage("description or userKey is required.");
    }
}

public sealed class GetResourceSuggestionsResponse
{
    public IReadOnlyList<ResourceSuggestionDto> Items { get; init; } = [];
}

public sealed class GetResourceSuggestionsQueryHandler(
    ResourceAccessService resourceAccessService,
    IResourceRepository resourceRepository,
    IValidator<GetResourceSuggestionsQuery> validator)
{
    public async Task<GetResourceSuggestionsResponse> ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        GetResourceSuggestionsQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            var message = string.Join(" | ", validationResult.Errors.Select(error => error.ErrorMessage));
            throw new ValidationAppException(message, "VALIDATION_ERROR");
        }

        await resourceAccessService.EnsureWriteAsync(actorUserId, projectId, cancellationToken);

        var items = await resourceRepository.GetSimilarAsync(
            projectId,
            query.Description,
            query.UserKey,
            10,
            cancellationToken);

        return new GetResourceSuggestionsResponse
        {
            Items = items.Select(ResourceMapper.ToSuggestionDto).ToList()
        };
    }
}
