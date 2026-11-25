using FluentValidation;

namespace DATN_BE.Modules.Workspaces.Application.Queries.GetWorkspaceAuditLogs;

public class GetWorkspaceAuditLogsQueryValidator : AbstractValidator<GetWorkspaceAuditLogsQuery>
{
    public GetWorkspaceAuditLogsQueryValidator()
    {
        RuleFor(q => q.WorkspaceId).NotEmpty();
        RuleFor(q => q.PageIndex).InclusiveBetween(1, 1000000);
        RuleFor(q => q.PageSize).InclusiveBetween(1, 100);
    }
}
