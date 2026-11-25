namespace DATN_BE.Shared.Application.Exceptions
{
    public class ForbiddenException : Exception
    {
        // 403
        public ForbiddenException(string message = "Bạn không có quyền thực hiện hành động này.") : base(message)
        {
            
        }
    }
}
