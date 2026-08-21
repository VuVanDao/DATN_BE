using FluentValidation;

namespace DATN_BE.Modules.Task.Application.Commands.AddCardComment
{
    public class AddCardCommentCommandValidator : AbstractValidator<AddCardCommentCommand>
    {
        public AddCardCommentCommandValidator()
        {
            RuleFor(x => x.CardId).NotEmpty();
            RuleFor(x => x.ContentText).NotEmpty().WithMessage("Nội dung bình luận không được để trống.");
        }
    }
}
