using FluentValidation;

namespace DATN_BE.Modules.Workspaces.Application.Commands.AddWorkspaceMember
{
    public class AddWorkspaceMemberCommandValidator : AbstractValidator<AddWorkspaceMemberCommand>
    {
        public AddWorkspaceMemberCommandValidator()
        {
            RuleFor(x => x.WorkspaceId)
                .NotEmpty().WithMessage("Mã Workspace không được để trống.");
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email thành viên không được để trống.")
                .EmailAddress().WithMessage("Email không đúng định dạng.");
        }
    }
}
