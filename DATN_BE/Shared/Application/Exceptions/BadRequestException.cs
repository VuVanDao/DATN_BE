namespace DATN_BE.Shared.Application.Exceptions
{
    public class BadRequestException : Exception
    {
        //exception 400
        public BadRequestException(string message) : base(message)
        {
            
        }
    }
}
