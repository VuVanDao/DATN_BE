using FluentValidation;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.CreateBoard
{
    public class CreateBoardCommandValidator : AbstractValidator<CreateBoardCommand>
    {
        public CreateBoardCommandValidator()
        {
            RuleFor(x => x.ProjectId)
                .NotEmpty().WithMessage("Mã Dự án (ProjectId) không hợp lệ.");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Tiêu đề Bảng không được để trống.")
                .MaximumLength(100).WithMessage("Tiêu đề Bảng không được vượt quá 100 ký tự.");

            RuleFor(x => x.Slug)
                .NotEmpty().WithMessage("Slug của Bảng không được để trống.")
                .MaximumLength(120).WithMessage("Slug của Bảng không được vượt quá 120 ký tự.");

            RuleFor(x => x.Type)
                .IsInEnum().WithMessage("Loại Bảng không hợp lệ.");
        }
    }
}
