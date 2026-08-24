using FluentValidation;

namespace DATN_BE.Modules.Identity.Application.Commands.UploadAvatar
{
    public class UploadAvatarCommandValidator : AbstractValidator<UploadAvatarCommand>
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB
        public UploadAvatarCommandValidator()
        {
            RuleFor(x => x.File)
                .NotNull().WithMessage("Vui lòng chọn file ảnh để tải lên.")
                .Must(file => file != null && file.Length > 0).WithMessage("File tải lên không được rỗng.")
                .Must(file => file != null && file.Length <= MaxFileSize).WithMessage("Dung lượng ảnh đại diện không vượt quá 5MB.")
                .Must(file =>
                {
                    if (file == null) return false;
                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    return AllowedExtensions.Contains(ext);
                }).WithMessage("Chỉ chấp nhận file ảnh có định dạng .jpg, .jpeg, .png, .webp.");
        }
    }
}
