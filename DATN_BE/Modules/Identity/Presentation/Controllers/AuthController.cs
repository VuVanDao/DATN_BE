using DATN_BE.Modules.Identity.Application.Commands.ForgotPassword;
using DATN_BE.Modules.Identity.Application.Commands.Login;
using DATN_BE.Modules.Identity.Application.Commands.RefrehToken;
using DATN_BE.Modules.Identity.Application.Commands.RegisterUser;
using DATN_BE.Modules.Identity.Application.Commands.ResetPassword;
using DATN_BE.Modules.Identity.Application.Commands.RevokeToken;
using DATN_BE.Modules.Identity.Application.DTOs;
using DATN_BE.Shared.Application.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using DATN_BE.Shared.Application.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace DATN_BE.Modules.Identity.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IWebHostEnvironment _environment;

        public AuthController(IMediator mediator, IWebHostEnvironment environment)
        {
            _mediator = mediator;
            _environment = environment;
        }
        // gửi một Refresh Token về trình duyệt của người dùng
        // và lưu nó vào bên trong Cookie với các thiết lập bảo mật nghiêm ngặt
        private void SetRefreshTokenCookie(string refreshToken, DateTimeOffset expires)
        {
            var isDevelopment = _environment.IsDevelopment();
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = !isDevelopment,
                SameSite = isDevelopment ? SameSiteMode.Lax : SameSiteMode.Strict,
                Expires = expires.UtcDateTime,
                Path = "/api/auth"
            };

            Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
        }

        private void DeleteRefreshTokenCookie()
        {
            var isDevelopment = _environment.IsDevelopment();
            Response.Cookies.Delete("refreshToken", new CookieOptions
            {
                HttpOnly = true,
                Secure = !isDevelopment,
                SameSite = isDevelopment ? SameSiteMode.Lax : SameSiteMode.Strict,
                Path = "/api/auth"
            });
        }

        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register([FromBody] RegisterUserCommand command)
        {
            var result = await _mediator.Send(command);
            SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAt);
            result.RefreshToken = string.Empty;
            return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Đăng kí tài khoản thành công"));
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginCommand command)
        {
            command.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            command.UserAgent = Request.Headers["User-Agent"].ToString();
            var result = await _mediator.Send(command);
            SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAt);
            result.RefreshToken = string.Empty;
            return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Đăng nhập thành công"));
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RefreshToken([FromBody] RefreshTokenCommand? command = null)
        {
            var refreshToken = Request.Cookies["refreshToken"] ?? command?.RefreshToken;
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new BadRequestException("Phiên đăng nhập đã hết hạn hoặc không hợp lệ. Vui lòng đăng nhập lại.");
            }

            var cmd = new RefreshTokenCommand
            {
                RefreshToken = refreshToken,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            var result = await _mediator.Send(cmd);
            SetRefreshTokenCookie(result.RefreshToken, result.RefreshTokenExpiresAt);
            result.RefreshToken = string.Empty;
            return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Cấp lại Token thành công."));
        }

        [HttpPost("revoke-token")]
        public async Task<ActionResult<ApiResponse<bool>>> RevokeToken([FromBody] RevokeTokenCommand? command = null)
        {
            var refreshToken = Request.Cookies["refreshToken"] ?? command?.RefreshToken;
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                await _mediator.Send(new RevokeTokenCommand
                {
                    RefreshToken = refreshToken,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                });
            }

            DeleteRefreshTokenCookie();
            return Ok(ApiResponse<bool>.Ok(true, "Thu hồi Token (Đăng xuất) thành công."));
        }
        /// <summary>
        /// Yêu cầu đặt lại mật khẩu khi quên mật khẩu
        /// </summary>
        [HttpPost("forgot-password")]
        public async Task<ActionResult<ApiResponse<string>>> ForgotPassword([FromBody] ForgotPasswordCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(ApiResponse<string>.Ok(result, "Yêu cầu đặt lại mật khẩu đã được xử lý."));
        }
        /// <summary>
        /// Xác nhận Token và đặt lại mật khẩu mới
        /// </summary>
        [HttpPost("reset-password")]
        public async Task<ActionResult<ApiResponse<bool>>> ResetPassword([FromBody] ResetPasswordCommand command)
        {
            await _mediator.Send(command);
            return Ok(ApiResponse<bool>.Ok(true, "Đặt lại mật khẩu thành công. Vui lòng đăng nhập với mật khẩu mới."));
        }
    }
}
