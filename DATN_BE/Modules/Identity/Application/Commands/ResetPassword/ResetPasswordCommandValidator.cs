using FluentValidation;

namespace DATN_BE.Modules.Identity.Application.Commands.ResetPassword
{
    public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email không được để trống.")
                .EmailAddress().WithMessage("Địa chỉ email không đúng định dạng.");
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Mã xác nhận (Token) không được để trống.");
            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("Vui lòng nhập mật khẩu mới.")
                .MinimumLength(6).WithMessage("Mật khẩu mới phải có ít nhất 6 ký tự.")
                .Matches("[A-Z]").WithMessage("Mật khẩu mới phải chứa ít nhất 1 chữ hoa.")
                .Matches("[a-z]").WithMessage("Mật khẩu mới phải chứa ít nhất 1 chữ thường.")
                .Matches("[0-9]").WithMessage("Mật khẩu mới phải chứa ít nhất 1 chữ số.")
                .Matches("[^a-zA-Z0-9]").WithMessage("Mật khẩu mới phải chứa ít nhất 1 ký tự đặc biệt.");
            RuleFor(x => x.ConfirmNewPassword)
                .Equal(x => x.NewPassword).WithMessage("Xác nhận mật khẩu mới không khớp.");
        }
    }
}
