using FluentValidation;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.CreateSprint
{
    public class CreateSprintCommandValidator : AbstractValidator<CreateSprintCommand>
    {
        public CreateSprintCommandValidator()
        {
            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("Mã Dự án không hợp lệ.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên Sprint không được để trống.")
                .MaximumLength(100).WithMessage("Tên Sprint không được vượt quá 100 ký tự.");

            RuleFor(x => x.Goal)
                .MaximumLength(500).WithMessage("Mục tiêu Sprint không được vượt quá 500 ký tự.")
                .When(x => !string.IsNullOrWhiteSpace(x.Goal));

            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate)
                .WithMessage("Ngày kết thúc phải lớn hơn ngày bắt đầu.")
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue);
        }
    }
}
