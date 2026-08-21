using FluentValidation;

namespace DATN_BE.Modules.Task.Application.Commands.UpdateCard
{
    public class UpdateCardCommandValidator : AbstractValidator<UpdateCardCommand>
    {
        public UpdateCardCommandValidator()
        {
            RuleFor(x => x.CardId).NotEmpty();
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ProgressPercentage).InclusiveBetween(0, 100).WithMessage("Tiến độ phải nằm trong khoảng từ 0% đến 100%.");
        }
    }
}
