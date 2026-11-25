using FluentValidation;

namespace DATN_BE.Modules.ProjectManagement.Application.Queries.GetMyProjects
{
    public sealed class GetMyProjectsQueryValidator : AbstractValidator<GetMyProjectsQuery>
    {
        public GetMyProjectsQueryValidator()
        {
            RuleFor(query => query.WorkspaceId)
                .Must(workspaceId => !workspaceId.HasValue || workspaceId.Value != Guid.Empty)
                .WithMessage("Mã Workspace (WorkspaceId) không hợp lệ.");
        }
    }
}
