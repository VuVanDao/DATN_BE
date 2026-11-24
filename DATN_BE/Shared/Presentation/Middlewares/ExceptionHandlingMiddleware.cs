using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Models;
using FluentValidation;
using System.Net;
using System.Text.Json;

namespace DATN_BE.Shared.Presentation.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);// Cho request đi qua toàn bộ hệ thống
            }
            catch(Exception ex) // Nếu ở bất kỳ đâu phát sinh lỗi (kể cả lỗi sập server)
            {
                _logger.LogError(ex, "Đã xảy ra ngoại lệ chưa xử lý: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }
        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            var response = exception switch
            {
                ValidationException validationEx => new
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = ApiResponse<object>.Fail("Dữ liệu đầu vào không hợp lệ.", validationEx.Errors.Select(e => e.ErrorMessage).ToList())
                },
                NotFoundException notFoundEx => new
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Body = ApiResponse<object>.Fail(notFoundEx.Message)
                },
                BadRequestException badRequestEx => new
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = ApiResponse<object>.Fail(badRequestEx.Message)
                },
                ForbiddenException forbiddenEx => new
                {
                    StatusCode = (int)HttpStatusCode.Forbidden,
                    Body = ApiResponse<object>.Fail(forbiddenEx.Message)
                },
                UnauthorizedException unauthorizedEx => new
                {
                    StatusCode = (int)HttpStatusCode.Unauthorized,
                    Body = ApiResponse<object>.Fail(unauthorizedEx.Message)
                },
                UnauthorizedAccessException unauthorizedAccessEx => new
                {
                    StatusCode = (int)HttpStatusCode.Unauthorized,
                    Body = ApiResponse<object>.Fail(unauthorizedAccessEx.Message)
                },
                _ => new
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Body = ApiResponse<object>.Fail("Đã xảy ra lỗi máy chủ nội bộ. Vui lòng thử lại sau.")
                }
            };
            context.Response.StatusCode = response.StatusCode;
            await context.Response.WriteAsync(JsonSerializer.Serialize(response.Body, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
        }
    }
}
