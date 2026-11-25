using FluentValidation;

namespace DATN_BE.Modules.Task.Application.Commands.AddCardDependency
{
    public class AddCardDependencyCommandValidator : AbstractValidator<AddCardDependencyCommand>
    {
        public AddCardDependencyCommandValidator()
        {
            RuleFor(x => x.CardId)
                .NotEmpty().WithMessage("CardId không được để trống.");

            RuleFor(x => x.TargetCardId)
                .NotEmpty().WithMessage("TargetCardId không được để trống.");

            RuleFor(x => x)
                .Must(x => x.CardId != x.TargetCardId)
                .WithMessage("Thẻ không thể tự tạo liên kết phụ thuộc với chính nó.");
        }
    }
}
