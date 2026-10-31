using FluentValidation;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.DeleteColumn;

public class DeleteColumnCommandValidator : AbstractValidator<DeleteColumnCommand>
{
    public DeleteColumnCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty().WithMessage("Mã Bảng (BoardId) không hợp lệ.");
        RuleFor(x => x.ColumnId).NotEmpty().WithMessage("Mã Cột (ColumnId) không hợp lệ.");
    }
}
