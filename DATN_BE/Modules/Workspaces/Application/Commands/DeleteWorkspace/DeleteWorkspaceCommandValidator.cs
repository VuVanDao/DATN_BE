using FluentValidation;

namespace DATN_BE.Modules.Workspaces.Application.Commands.DeleteWorkspace
{
    public class DeleteWorkspaceCommandValidator : AbstractValidator<DeleteWorkspaceCommand>
    {
        public DeleteWorkspaceCommandValidator()
        {
            RuleFor(x => x.WorkspaceId)
                .NotEmpty().WithMessage("Mã WorkspaceId không được để trống.");
        }
    }
}
