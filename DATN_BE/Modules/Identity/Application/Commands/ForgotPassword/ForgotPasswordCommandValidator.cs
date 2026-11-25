using FluentValidation;

namespace DATN_BE.Modules.Identity.Application.Commands.ForgotPassword
{
    public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
    {
        public ForgotPasswordCommandValidator()
        {
            RuleFor(x => x.Email)
               .NotEmpty().WithMessage("Email không được để trống.")
               .EmailAddress().WithMessage("Địa chỉ email không đúng định dạng.");
        }
    }
}
