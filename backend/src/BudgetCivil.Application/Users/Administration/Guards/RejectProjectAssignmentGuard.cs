using System.Text.Json;
using BudgetCivil.Application.Common.Exceptions;

namespace BudgetCivil.Application.Users.Administration.Guards;

public sealed class RejectProjectAssignmentGuard
{
    private static readonly HashSet<string> ForbiddenFieldNames =
    [
        "project_id",
        "project_ids",
        "user_project_id",
        "user_project_ids",
        "projects",
        "project"
    ];

    public void EnsureNoProjectAssignment(IDictionary<string, JsonElement>? extensionData)
    {
        if (extensionData is null || extensionData.Count == 0)
        {
            return;
        }

        var includesProjectAssignment = extensionData.Keys
            .Select(key => key.Trim().ToLowerInvariant())
            .Any(normalizedKey =>
                ForbiddenFieldNames.Contains(normalizedKey) ||
                normalizedKey.Contains("project"));

        if (!includesProjectAssignment)
        {
            return;
        }

        throw new ValidationAppException(
            "User-project assignment is out of scope for this feature.",
            "USER_PROJECT_ASSIGNMENT_OUT_OF_SCOPE");
    }
}
