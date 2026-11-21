using FluentValidation;

namespace DATN_BE.Modules.Task.Application.Commands.CreateBacklogCard
{
    public class CreateBacklogCardCommandValidator : AbstractValidator<CreateBacklogCardCommand>
    {
        public CreateBacklogCardCommandValidator()
        {
            RuleFor(x => x.BoardId)
                .NotEmpty().WithMessage("Mã Bảng Scrum (BoardId) không được để trống.");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Tiêu đề công việc không được để trống.")
                .MaximumLength(200).WithMessage("Tiêu đề công việc không được vượt quá 200 ký tự.");

            RuleFor(x => x.StoryPoints)
                .GreaterThanOrEqualTo(0).When(x => x.StoryPoints.HasValue)
                .WithMessage("Điểm Story Points không được âm.");
        }
    }
}
