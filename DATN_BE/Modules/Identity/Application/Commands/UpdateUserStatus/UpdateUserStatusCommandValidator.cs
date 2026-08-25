using FluentValidation;

namespace DATN_BE.Modules.Identity.Application.Commands.UpdateUserStatus
{
    public class UpdateUserStatusCommandValidator : AbstractValidator<UpdateUserStatusCommand>
    {
        public UpdateUserStatusCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId không được để trống.");
        }
    }
}
