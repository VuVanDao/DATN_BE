using FluentValidation;

namespace DATN_BE.Modules.Identity.Application.Commands.AdminUpdateUser
{
    public class AdminUpdateUserCommandValidator : AbstractValidator<AdminUpdateUserCommand>
    {
        public AdminUpdateUserCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId không được để trống.");

            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Tên người dùng không được để trống.")
                .MinimumLength(3).WithMessage("Tên người dùng phải từ 3 ký tự trở lên.")
                .MaximumLength(100).WithMessage("Tên người dùng không được vượt quá 100 ký tự.");
        }
    }
}
