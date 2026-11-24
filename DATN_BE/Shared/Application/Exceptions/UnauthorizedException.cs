namespace DATN_BE.Shared.Application.Exceptions
{
    public class UnauthorizedException : Exception
    {
        // 401
        public UnauthorizedException(string message = "Bạn cần đăng nhập để thực hiện hành động này.") : base(message)
        {
        }
    }
}
