using FluentValidation;

namespace DATN_BE.Modules.Task.Application.Commands.CreateCard
{
    public class CreateCardCommandValidator : AbstractValidator<CreateCardCommand>
    {
        public CreateCardCommandValidator()
        {
            RuleFor(x => x.ColumnId)
            .NotEmpty().WithMessage("Mã Cột (ColumnId) không hợp lệ.");
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề Thẻ không được để trống.")
            .MaximumLength(200).WithMessage("Tiêu đề Thẻ không được vượt quá 200 ký tự.");
        RuleFor(x => x.StoryPoints)
            .GreaterThanOrEqualTo(0).When(x => x.StoryPoints.HasValue)
            .WithMessage("Điểm Story Points không được âm.");
        RuleFor(x => x.DueDate)
            .GreaterThanOrEqualTo(x => x.StartDate!.Value)
            .When(x => x.StartDate.HasValue && x.DueDate.HasValue)
            .WithMessage("Hạn chót (DueDate) phải lớn hơn hoặc bằng Ngày bắt đầu (StartDate).");
        }
    }
}
