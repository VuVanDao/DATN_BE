using FluentValidation;

namespace DATN_BE.Modules.Identity.Application.Commands.UpdateProfile
{
    public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
    {
        public UpdateProfileCommandValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username không được để trống.")
                .MinimumLength(3).WithMessage("Username phải có ít nhất 3 ký tự.")
                .MaximumLength(50).WithMessage("Username không vượt quá 50 ký tự.")
                .Matches("^[a-zA-Z0-9_.-]+$").WithMessage("Username chỉ được chứa chữ cái, số, dấu chấm, gạch ngang và gạch dưới.");
            RuleFor(x => x.Bio)
                .MaximumLength(500).WithMessage("Bio không vượt quá 500 ký tự.");
        }
    }
}
