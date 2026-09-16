using FluentValidation;

namespace DATN_BE.Modules.Identity.Application.Commands.AdminDeleteUser
{
    public class AdminDeleteUserCommandValidator : AbstractValidator<AdminDeleteUserCommand>
    {
        public AdminDeleteUserCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId không được để trống.");
        }
    }
}
