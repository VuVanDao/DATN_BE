using DATN_BE.Modules.Identity.Application.Commands.ChangePassword;
using DATN_BE.Modules.Identity.Application.Commands.UpdateProfile;
using DATN_BE.Modules.Identity.Application.DTOs;
using DATN_BE.Modules.Identity.Application.Queries.GetProfile;
using DATN_BE.Shared.Application.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DATN_BE.Modules.Identity.Presentation.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;
        public UsersController(IMediator mediator)
        {
            _mediator = mediator;
        }
        /// <summary>
        /// Lấy thông tin cá nhân của người dùng đang đăng nhập
        /// </summary>
        [HttpGet("me")]
        public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetProfile()
        {
            var result = await _mediator.Send(new GetProfileQuery());
            return Ok(ApiResponse<UserProfileDto>.Ok(result, "Lấy thông tin cá nhân thành công."));
        }
        /// <summary>
        /// Cập nhật thông tin cá nhân (Username, Bio)
        /// </summary>
        [HttpPut("me")]
        public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateProfile([FromBody] UpdateProfileCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(ApiResponse<UserProfileDto>.Ok(result, "Cập nhật thông tin cá nhân thành công."));
        }
        /// <summary>
        /// Đổi mật khẩu
        /// </summary>
        [HttpPost("change-password")]
        public async Task<ActionResult<ApiResponse<bool>>> ChangePassword([FromBody] ChangePasswordCommand command)
        {
            await _mediator.Send(command);
            return Ok(ApiResponse<bool>.Ok(true, "Đổi mật khẩu thành công. Vui lòng sử dụng mật khẩu mới cho các lần đăng nhập sau."));
        }
    }
}
