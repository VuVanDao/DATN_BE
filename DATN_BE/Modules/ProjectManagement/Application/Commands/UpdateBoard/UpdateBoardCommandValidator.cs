using FluentValidation;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateBoard
{
    public class UpdateBoardCommandValidator : AbstractValidator<UpdateBoardCommand>
    {
        public UpdateBoardCommandValidator()
        {
            RuleFor(x => x.BoardId)
                .NotEmpty().WithMessage("Mã Bảng (BoardId) không hợp lệ.");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Tiêu đề Bảng không được để trống.")
                .MaximumLength(100).WithMessage("Tiêu đề Bảng không được vượt quá 100 ký tự.");

            RuleFor(x => x.Slug)
                .MaximumLength(120).WithMessage("Slug của Bảng không được vượt quá 120 ký tự.")
                .When(x => !string.IsNullOrWhiteSpace(x.Slug));

            RuleFor(x => x.Type)
                .IsInEnum().WithMessage("Loại Bảng không hợp lệ.");
        }
    }
}
