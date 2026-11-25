namespace DATN_BE.Shared.Application.Interfaces
{
    // Interface lấy thông tin User đang đăng nhập
    public interface ICurrentUserService
    {
        Guid? UserId { get; }
        string? Email { get; }
        string? Username { get; }
        bool IsAuthenticated { get; }
    }
}
