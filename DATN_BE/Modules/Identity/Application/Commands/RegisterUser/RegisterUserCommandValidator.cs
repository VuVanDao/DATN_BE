using FluentValidation;

namespace DATN_BE.Modules.Identity.Application.Commands.RegisterUser
{
    public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
    {
        public RegisterUserCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email không được để trống")
                .EmailAddress().WithMessage("Email không đúng định dạng")
                .MaximumLength(255).WithMessage("Emial không được vượt quá");
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Tên người dùng không được để trống.")
                .MinimumLength(3).WithMessage("Tên người dùng phải từ 3 ký tự trở lên.")
                .MaximumLength(100).WithMessage("Tên người dùng không được vượt quá 100 ký tự.");
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Mật khẩu không được để trống.")
                .MinimumLength(6).WithMessage("Mật khẩu phải từ 6 ký tự trở lên.")
                .MaximumLength(50).WithMessage("Mật khẩu không được vượt quá 50 ký tự.");
        }
    }
}
