using DATN_BE.Modules.Identity.Application.Commands.UpdateUserStatus;
using DATN_BE.Modules.Identity.Application.DTOs;
using DATN_BE.Modules.Identity.Application.Queries.GetAdminUsers;
using DATN_BE.Shared.Application.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DATN_BE.Modules.Identity.Presentation.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/admin/users")]
    public class AdminUsersController : ControllerBase
    {
        private readonly IMediator _mediator;
        public AdminUsersController(IMediator mediator)
        {
            _mediator = mediator;
        }
        /// <summary>
        /// Lấy danh sách tất cả người dùng (Có phân trang, tìm kiếm và lọc trạng thái)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<AdminUserDto>>>> GetUsers([FromQuery] GetAdminUsersQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(ApiResponse<PagedResult<AdminUserDto>>.Ok(result, "Lấy danh sách người dùng thành công."));
        }
        /// <summary>
        /// Kích hoạt hoặc Khóa tài khoản người dùng
        /// </summary>
        [HttpPatch("{id:guid}/status")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateUserStatus(Guid id, [FromBody] UpdateUserStatusCommand command)
        {
            command.UserId = id;
            await _mediator.Send(command);
            var message = command.IsActive ? "Mở khóa tài khoản thành công." : "Khóa tài khoản thành công.";
            return Ok(ApiResponse<bool>.Ok(true, message));
        }
    }
}
