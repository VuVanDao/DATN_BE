using DATN_BE.Modules.Workspaces.Application.DTOs;
using FluentValidation;

namespace DATN_BE.Modules.Workspaces.Application.Commands.CreateWorkspace
{
    public class CreateWorkspaceCommandValidator : AbstractValidator<CreateWorkspaceCommand>
    {
        public CreateWorkspaceCommandValidator()
        {
            RuleFor(x => x.Name)
               .NotEmpty().WithMessage("Tên Workspace không được để trống.")
               .MinimumLength(3).WithMessage("Tên Workspace phải có ít nhất 3 ký tự.")
               .MaximumLength(20).WithMessage("Tên Workspace không được vượt quá 100 ký tự.");
            RuleFor(x => x.Description)
               .MaximumLength(500).WithMessage("Mô tả không được vượt quá 500 ký tự.");
        }
    }
}
