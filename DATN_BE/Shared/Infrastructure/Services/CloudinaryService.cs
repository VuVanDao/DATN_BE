using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace DATN_BE.Shared.Infrastructure.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        public CloudinaryService(IConfiguration configuration)
        {
            var cloudName = configuration["CloudinarySettings:CloudName"];
            var apiKey = configuration["CloudinarySettings:ApiKey"];
            var apiSecret = configuration["CloudinarySettings:ApiSecret"];
            if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            {
                throw new InvalidOperationException("Chưa cấu hình đầy đủ thông tin Cloudinary (CloudName, ApiKey, ApiSecret).");
            }
            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true;
        }
        public async Task<string> UploadAvatarAsync(IFormFile file, string userId, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
            {
                throw new BadRequestException("File tải lên không hợp lệ hoặc rỗng.");
            }
            await using var stream = file.OpenReadStream();
            var uploadparams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "TrelloClone/Avatars",
                // Đặt PublicId theo userId để mỗi lần user đổi avatar mới sẽ ghi đè lên ảnh cũ 
                PublicId = $"avatar_{userId}",
                Overwrite = true,
                // Tự động nhận diện khuôn mặt và crop vuông 250x250
                Transformation = new Transformation()
                    .Width(250)
                    .Height(250)
                    .Crop("thumb")
                    .Gravity("face")
                    .Quality("auto")
                    .FetchFormat("auto")
            };
            var uploadResult = await _cloudinary.UploadAsync(uploadparams, cancellationToken);
            if (uploadResult.Error != null)
            {
                throw new BadRequestException($"Lỗi upload ảnh lên Cloudinary: {uploadResult.Error.Message}");
            }
            return uploadResult.SecureUrl.ToString();
        }

        public async Task<string> UploadWorkspaceImageAAsync(IFormFile file, string workspaceId, string imgType, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
            {
                throw new BadRequestException("File tải lên không hợp lệ hoặc rỗng.");
            }
            var isLogo = imgType.Equals("logo",StringComparison.OrdinalIgnoreCase);
            var folder = isLogo ? "TrelloClone/WorkspaceLogos" : "TrelloClone/WorkspaceCovers";
            var publicId = isLogo ? $"ws_logo_{workspaceId}" : $"ws_cover_{workspaceId}";

            await using var stream = file.OpenReadStream();
            var uploadparams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                PublicId = publicId,
                Overwrite = true,
                Transformation = isLogo ? new Transformation().Width(250)
                            .Height(250)
                            .Crop("thumb")
                            .Quality("auto")
                            .FetchFormat("auto"): new Transformation()
                                                .Width(1200)
                                                .Crop("limit")
                                                .Quality("auto")
                                                .FetchFormat("auto")
                
            };
            var uploadResult = await _cloudinary.UploadAsync(uploadparams, cancellationToken);
            if (uploadResult.Error != null)
            {
                throw new BadRequestException($"Lỗi upload ảnh lên Cloudinary: {uploadResult.Error.Message}");
            }
            return uploadResult.SecureUrl.ToString();
        }
    }
}
