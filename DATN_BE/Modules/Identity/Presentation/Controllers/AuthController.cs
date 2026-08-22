using DATN_BE.Modules.Identity.Application.Commands.Login;
using DATN_BE.Modules.Identity.Application.Commands.RefrehToken;
using DATN_BE.Modules.Identity.Application.Commands.RegisterUser;
using DATN_BE.Modules.Identity.Application.Commands.RevokeToken;
using DATN_BE.Modules.Identity.Application.DTOs;
using DATN_BE.Modules.Identity.Domain.Entities;
using DATN_BE.Shared.Application.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DATN_BE.Modules.Identity.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register([FromBody] RegisterUserCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(ApiResponse<AuthResponseDto>.Ok(result,"Đăng kí tài khoản thành công"));
        }
        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Đăng nhập thành công"));
        }
        [Authorize]
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var email = User.FindFirstValue(ClaimTypes.Email); 
            var username = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("unique_name");

            return Ok(ApiResponse<object>.Ok(new
            {
                Id = userId,
                Email = email,
                Username = username
            }, "Lấy thông tin tài khoản hiện tại thành công."));
        }
        [HttpPost("refresh-token")]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RefreshToken([FromBody] RefreshTokenCommand command)
        {
            command.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _mediator.Send(command);
            return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Cấp lại Token thành công."));
        }

        [HttpPost("revoke-token")]
        public async Task<ActionResult<ApiResponse<bool>>> RevokeToken([FromBody] RevokeTokenCommand command)
        {
            command.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _mediator.Send(command);
            return Ok(ApiResponse<bool>.Ok(true, "Thu hồi Token (Đăng xuất) thành công."));
        }
    }
}
