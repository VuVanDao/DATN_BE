using FluentValidation;

namespace DATN_BE.Modules.ProjectManagement.Application.Queries.GetWorkspaceProjects;

public class GetWorkspaceProjectsQueryValidator : AbstractValidator<GetWorkspaceProjectsQuery>
{
    public GetWorkspaceProjectsQueryValidator()
    {
        RuleFor(q => q.WorkspaceId).NotEmpty();
        RuleFor(q => q.PageIndex).InclusiveBetween(1, 1000000);
        RuleFor(q => q.PageSize).InclusiveBetween(1, 100);
    }
}
