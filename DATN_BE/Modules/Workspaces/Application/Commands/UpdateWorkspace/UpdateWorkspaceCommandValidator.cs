using FluentValidation;

namespace DATN_BE.Modules.Workspaces.Application.Commands.UpdateWorkspace
{
    public class UpdateWorkspaceCommandValidator: AbstractValidator<UpdateWorkspaceCommand>
    {
        public UpdateWorkspaceCommandValidator()
        {
            RuleFor(x => x.WorkspaceId)
                .NotEmpty().WithMessage("Mã Workspace không được để trống.");
            RuleFor(x => x.Name)
                .MinimumLength(3).WithMessage("Tên Workspace phải có ít nhất 3 ký tự.")
                .MaximumLength(100).WithMessage("Tên Workspace không được vượt quá 100 ký tự.")
                .When(x => !string.IsNullOrWhiteSpace(x.Name));
            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Mô tả không được vượt quá 500 ký tự.")
                .When(x => !string.IsNullOrWhiteSpace(x.Description));
        }
    }
}
