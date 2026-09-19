namespace DATN_BE.Shared.Application.Interfaces
{
    public interface ICloudinaryService
    {
        Task<string> UploadAvatarAsync(IFormFile file, string userId, CancellationToken cancellationToken = default);
        Task<string> UploadWorkspaceImageAsync(IFormFile file, string workspaceId, string imgType, CancellationToken cancellationToken = default);
    }
}
