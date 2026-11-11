using DATN_BE.Modules.Identity.Application.Commands.AdminCreateUser;
using DATN_BE.Modules.Identity.Application.Commands.AdminDeleteUser;
using DATN_BE.Modules.Identity.Application.Commands.AdminUpdateUser;
using DATN_BE.Modules.Identity.Application.Commands.AssignUserRoles;
using DATN_BE.Modules.Identity.Application.Commands.UpdateUserStatus;
using DATN_BE.Modules.Identity.Application.DTOs;
using DATN_BE.Modules.Identity.Application.Queries.GetRoles;
using DATN_BE.Modules.Identity.Application.Queries.GetUsers;
using DATN_BE.Shared.Application.Models;
using DATN_BE.Shared.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DATN_BE.Modules.Identity.Presentation.Controllers
{
    [Authorize(Roles = RoleConstants.SystemAdmin)]
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AdminController(IMediator mediator)
        {
            _mediator = mediator;
        }

        #region --- 1. User Management ---

        /// <summary>
        /// [Admin] Lấy danh sách người dùng toàn hệ thống (Phân trang, tìm kiếm, lọc)
        /// </summary>
        [HttpGet("users")]
        public async Task<ActionResult<ApiResponse<PagedResult<UserManagementDto>>>> GetUsers([FromQuery] GetUsersQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(ApiResponse<PagedResult<UserManagementDto>>.Ok(result, "Lấy danh sách người dùng thành công."));
        }

        /// <summary>
        /// [Admin] Khóa hoặc Mở khóa tài khoản người dùng
        /// </summary>
        [HttpPatch("users/{id:guid}/status")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateUserStatus(Guid id, [FromBody] UpdateUserStatusCommand command)
        {
            command.UserId = id;
            await _mediator.Send(command);
            var message = command.IsActive ? "Mở khóa tài khoản thành công." : "Khóa tài khoản thành công.";
            return Ok(ApiResponse<bool>.Ok(true, message));
        }

        /// <summary>
        /// [Admin] Gán vai trò (Role) cho một người dùng
        /// </summary>
        [HttpPost("users/{id:guid}/role")]
        public async Task<ActionResult<ApiResponse<bool>>> AssignRole(Guid id, [FromBody] AssignUserRoleCommand command)
        {
            command.UserId = id;
            await _mediator.Send(command);
            return Ok(ApiResponse<bool>.Ok(true, "Cập nhật vai trò cho người dùng thành công."));
        }

        /// <summary>
        /// [Admin] Tạo mới một tài khoản người dùng (Role mặc định: User)
        /// </summary>
        [HttpPost("users")]
        public async Task<ActionResult<ApiResponse<Guid>>> CreateUser([FromBody] AdminCreateUserCommand command)
        {
            var userId = await _mediator.Send(command);
            return Ok(ApiResponse<Guid>.Ok(userId, "Tạo người dùng thành công."));
        }

        /// <summary>
        /// [Admin] Cập nhật thông tin người dùng (Username, Bio)
        /// </summary>
        [HttpPut("users/{id:guid}")]
        public async Task<ActionResult<ApiResponse<UserManagementDto>>> UpdateUser(Guid id, [FromBody] AdminUpdateUserCommand command)
        {
            command.UserId = id;
            var result = await _mediator.Send(command);
            return Ok(ApiResponse<UserManagementDto>.Ok(result, "Cập nhật thông tin người dùng thành công."));
        }

        /// <summary>
        /// [Admin] Xóa mềm tài khoản người dùng (IsDeleted = true)
        /// </summary>
        [HttpDelete("users/{id:guid}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteUser(Guid id)
        {
            await _mediator.Send(new AdminDeleteUserCommand { UserId = id });
            return Ok(ApiResponse<bool>.Ok(true, "Xóa người dùng thành công."));
        }

        #endregion

        #region --- 2. Roles Management ---

        /// <summary>
        /// [Admin] Lấy danh sách tất cả các Role trong hệ thống
        /// </summary>
        [HttpGet("roles")]
        public async Task<ActionResult<ApiResponse<List<RoleDto>>>> GetRoles()
        {
            var result = await _mediator.Send(new GetRolesQuery());
            return Ok(ApiResponse<List<RoleDto>>.Ok(result, "Lấy danh sách vai trò thành công."));
        }

        #endregion
    }
}
