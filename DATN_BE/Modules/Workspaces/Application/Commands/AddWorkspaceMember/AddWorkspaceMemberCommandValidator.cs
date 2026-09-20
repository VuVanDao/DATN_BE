using FluentValidation;

namespace DATN_BE.Modules.Workspaces.Application.Commands.AddWorkspaceMember
{
    public class AddWorkspaceMemberCommandValidator : AbstractValidator<AddWorkspaceMemberCommand>
    {
        public AddWorkspaceMemberCommandValidator()
        {
            RuleFor(x => x.WorkspaceId)
                .NotEmpty().WithMessage("Mã Workspace không được để trống");
            RuleFor(x => x.Members)
                .NotEmpty().WithMessage("Danh sách thành viên không được để trống")
                .Must(m => m.Count >= 1).WithMessage("Cần ít nhất 1 thành viên để thêm")
                .Must(m => m.Count <= 10).WithMessage("Chỉ được thêm tối đa 10 thành viên mỗi lần");
            RuleForEach(x => x.Members)
                .ChildRules(member =>
                {
                    member.RuleFor(m => m.Email)
                      .NotEmpty().WithMessage("Email thành viên không được để trống")
                      .EmailAddress().WithMessage("Email không đúng định dạng");
                });
        }
    }
}
