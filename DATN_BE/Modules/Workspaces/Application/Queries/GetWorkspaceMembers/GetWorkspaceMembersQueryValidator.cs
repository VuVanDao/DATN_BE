using FluentValidation;

namespace DATN_BE.Modules.Workspaces.Application.Queries.GetWorkspaceMembers;

public class GetWorkspaceMembersQueryValidator : AbstractValidator<GetWorkspaceMembersQuery>
{
    public GetWorkspaceMembersQueryValidator()
    {
        RuleFor(q => q.WorkspaceId).NotEmpty();
        RuleFor(q => q.PageIndex).InclusiveBetween(1, 1000000);
        RuleFor(q => q.PageSize).InclusiveBetween(1, 100);
    }
}
