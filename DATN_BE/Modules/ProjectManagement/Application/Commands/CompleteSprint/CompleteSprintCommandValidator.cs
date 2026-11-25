using FluentValidation;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.CompleteSprint
{
    public class CompleteSprintCommandValidator : AbstractValidator<CompleteSprintCommand>
    {
        public CompleteSprintCommandValidator()
        {
            RuleFor(x => x.SprintId)
                .NotEmpty().WithMessage("Mã Sprint không hợp lệ.");
        }
    }
}
