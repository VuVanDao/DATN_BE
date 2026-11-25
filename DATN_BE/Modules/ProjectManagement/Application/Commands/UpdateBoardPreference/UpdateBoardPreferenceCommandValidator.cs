using FluentValidation;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateBoardPreference;

public class UpdateBoardPreferenceCommandValidator : AbstractValidator<UpdateBoardPreferenceCommand>
{
    public UpdateBoardPreferenceCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x).Must(x => x.IsPinned.HasValue || x.MarkViewed)
            .WithMessage("Cần cung cấp IsPinned hoặc bật MarkViewed.");
    }
}
