using FluentValidation;

namespace DATN_BE.Modules.Identity.Application.Commands.RefrehToken
{
    public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
    {
        public RefreshTokenCommandValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage("Refresh token không được để trống.");
        }
    }
}
