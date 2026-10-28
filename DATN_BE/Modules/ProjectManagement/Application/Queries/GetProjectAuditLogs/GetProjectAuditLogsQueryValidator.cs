using FluentValidation;

namespace DATN_BE.Modules.ProjectManagement.Application.Queries.GetProjectAuditLogs;

public class GetProjectAuditLogsQueryValidator : AbstractValidator<GetProjectAuditLogsQuery>
{
    public GetProjectAuditLogsQueryValidator()
    {
        RuleFor(q => q.ProjectId).NotEmpty();
        RuleFor(q => q.PageIndex).InclusiveBetween(1, 1000000);
        RuleFor(q => q.PageSize).InclusiveBetween(1, 100);
    }
}
