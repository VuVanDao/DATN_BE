using FluentValidation;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.DeleteSprint
{
    public class DeleteSprintCommandValidator : AbstractValidator<DeleteSprintCommand>
    {
        public DeleteSprintCommandValidator()
        {
            RuleFor(x => x.SprintId)
                .NotEmpty().WithMessage("Mã Sprint không hợp lệ.");
        }
    }
}
