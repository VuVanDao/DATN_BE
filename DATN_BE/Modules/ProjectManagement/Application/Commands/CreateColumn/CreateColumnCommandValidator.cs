using FluentValidation;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.CreateColumn
{
    public class CreateColumnCommandValidator : AbstractValidator<CreateColumnCommand>
    {
        public CreateColumnCommandValidator()
        {
            RuleFor(x => x.BoardId)
                .NotEmpty().WithMessage("Mã Bảng (BoardId) không hợp lệ.");
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Tiêu đề Cột không được để trống.")
                .MaximumLength(100).WithMessage("Tiêu đề Cột không được vượt quá 100 ký tự.");
        }
    }
}
