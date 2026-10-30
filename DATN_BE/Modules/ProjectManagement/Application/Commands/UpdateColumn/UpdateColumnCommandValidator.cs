using FluentValidation;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateColumn;

public class UpdateColumnCommandValidator : AbstractValidator<UpdateColumnCommand>
{
    public UpdateColumnCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty().WithMessage("Mã Bảng (BoardId) không hợp lệ.");
        RuleFor(x => x.ColumnId).NotEmpty().WithMessage("Mã Cột (ColumnId) không hợp lệ.");
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề Cột không được để trống.")
            .MaximumLength(100).WithMessage("Tiêu đề Cột không được vượt quá 100 ký tự.");
    }
}
