using FluentValidation;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.CreateProject
{
    public class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
    {
        public CreateProjectCommandValidator()
        {
            RuleFor(x => x.WorkspaceId)
                .NotEmpty().WithMessage("Mã Không gian làm việc (WorkspaceId) không được để trống.");
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên dự án không được để trống.")
                .MinimumLength(2).WithMessage("Tên dự án phải từ 2 ký tự trở lên.")
                .MaximumLength(100).WithMessage("Tên dự án không được vượt quá 100 ký tự.");
            RuleFor(x => x.ProjectKey)
                .NotEmpty().WithMessage("Mã viết tắt dự án (ProjectKey) không được để trống.")
                .MinimumLength(2).WithMessage("ProjectKey phải có ít nhất 2 ký tự.")
                .MaximumLength(10).WithMessage("ProjectKey không được vượt quá 10 ký tự.")
                .Matches("^[a-zA-Z0-9]+$").WithMessage("ProjectKey chỉ được chứa chữ cái và số, không chứa khoảng trắng.");
            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Mô tả không được vượt quá 500 ký tự.");
        }
    }
}
