using BudgetCivil.Application.Wbs.Dtos;
using BudgetCivil.Application.Wbs.Repositories;
using BudgetCivil.Application.Wbs.Services;
using BudgetCivil.Domain.Wbs;

namespace BudgetCivil.Application.Wbs.Queries;

public sealed class ValidateWbsCodeQuery
{
    public string? Code { get; init; }

    public Guid? ExcludeNodeId { get; init; }
}

/// <summary>
/// Valida formato y disponibilidad del código antes del alta o la edición, sin crear ni modificar datos
/// (`REQ-EDT-17`, `SC-EDT-58`…`61`, `D-115`).
/// </summary>
public sealed class ValidateWbsCodeQueryHandler(
    WbsAccessService wbsAccessService,
    IWbsRepository wbsRepository)
{
    public const string CodeInvalid = "WBS_CODE_INVALID";
    public const string CodeDuplicated = "WBS_CODE_DUPLICATED";

    public async Task<ValidateWbsCodeResponse> ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        ValidateWbsCodeQuery query,
        CancellationToken cancellationToken = default)
    {
        await wbsAccessService.EnsureWriteAsync(actorUserId, projectId, cancellationToken);

        var code = query.Code?.Trim() ?? string.Empty;
        if (!WbsCodePolicy.IsValidCode(code))
        {
            return new ValidateWbsCodeResponse
            {
                Code = code,
                IsValid = false,
                IsAvailable = false,
                Reason = CodeInvalid
            };
        }

        Guid? excludeCodeId = null;
        if (query.ExcludeNodeId.HasValue)
        {
            excludeCodeId = await wbsRepository.GetNodeCodeIdAsync(
                projectId,
                query.ExcludeNodeId.Value,
                cancellationToken);
        }

        var exists = await wbsRepository.CodeExistsAsync(
            projectId,
            code,
            excludeCodeId,
            cancellationToken);

        return new ValidateWbsCodeResponse
        {
            Code = code,
            IsValid = true,
            IsAvailable = !exists,
            Reason = exists ? CodeDuplicated : null
        };
    }
}
