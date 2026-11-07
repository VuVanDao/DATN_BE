using FluentValidation;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.StartSprint
{
    public class StartSprintCommandValidator : AbstractValidator<StartSprintCommand>
    {
        public StartSprintCommandValidator()
        {
            RuleFor(x => x.SprintId)
                .NotEmpty().WithMessage("Mã Sprint không hợp lệ.");

            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate)
                .WithMessage("Ngày kết thúc phải lớn hơn ngày bắt đầu.")
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue);
        }
    }
}
