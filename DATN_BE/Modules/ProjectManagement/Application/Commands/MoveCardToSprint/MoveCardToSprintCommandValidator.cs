using FluentValidation;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.MoveCardToSprint
{
    public class MoveCardToSprintCommandValidator : AbstractValidator<MoveCardToSprintCommand>
    {
        public MoveCardToSprintCommandValidator()
        {
            RuleFor(x => x.CardId)
                .NotEmpty().WithMessage("Mã thẻ không hợp lệ.");
        }
    }
}
