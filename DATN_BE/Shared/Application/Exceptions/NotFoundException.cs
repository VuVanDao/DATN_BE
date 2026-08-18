namespace DATN_BE.Shared.Application.Exceptions
{
    public class NotFoundException : Exception
    {
        // exception 404
        public NotFoundException(string message) : base(message)
        {
            
        }
        public NotFoundException(string entityName, object key) : base($"Không tìm thấy {entityName} với mã định danh: {key}")
        {
            
        }
    }
}
