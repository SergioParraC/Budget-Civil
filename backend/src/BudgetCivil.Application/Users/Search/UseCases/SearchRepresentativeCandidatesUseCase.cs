using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Users.Search.Repositories;

namespace BudgetCivil.Application.Users.Search.UseCases;

/// <summary>
/// Candidatos a representante legal para el alta o la edición de una compañía (`D-87`). El tipo y el
/// número de documento son obligatorios; el nombre de usuario y el correo son filtros opcionales.
/// </summary>
public sealed class SearchRepresentativeCandidatesUseCase(IUserSearchRepository repository)
{
    public Task<IReadOnlyList<UserSearchResult>> ExecuteAsync(
        Guid actorUserId,
        UserSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.DocumentType) || string.IsNullOrWhiteSpace(query.DocumentNumber))
        {
            throw new ValidationAppException(
                "documentType and documentNumber are required.",
                "VALIDATION_ERROR");
        }

        return repository.SearchRepresentativeCandidatesAsync(actorUserId, query, cancellationToken);
    }
}
