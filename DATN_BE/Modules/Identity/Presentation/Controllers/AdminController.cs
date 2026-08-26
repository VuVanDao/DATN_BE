using DATN_BE.Modules.Identity.Application.Commands.AssignUserRoles;
using DATN_BE.Modules.Identity.Application.Commands.UpdateUserStatus;
using DATN_BE.Modules.Identity.Application.DTOs;
using DATN_BE.Modules.Identity.Application.Queries.GetAdminUsers;
using DATN_BE.Modules.Identity.Application.Queries.GetPermissions;
using DATN_BE.Modules.Identity.Application.Queries.GetRoles;
using DATN_BE.Shared.Application.Models;
using DATN_BE.Shared.Infrastructure.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DATN_BE.Modules.Identity.Presentation.Controllers
{
    [Authorize]
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
        [HasPermission("user:read")]
        public async Task<ActionResult<ApiResponse<PagedResult<AdminUserDto>>>> GetUsers([FromQuery] GetAdminUsersQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(ApiResponse<PagedResult<AdminUserDto>>.Ok(result, "Lấy danh sách người dùng thành công."));
        }

        /// <summary>
        /// [Admin] Khóa hoặc Mở khóa tài khoản người dùng
        /// </summary>
        [HttpPatch("users/{id:guid}/status")]
        [HasPermission("user:manage")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateUserStatus(Guid id, [FromBody] UpdateUserStatusCommand command)
        {
            command.UserId = id;
            await _mediator.Send(command);
            var message = command.IsActive ? "Mở khóa tài khoản thành công." : "Khóa tài khoản thành công.";
            return Ok(ApiResponse<bool>.Ok(true, message));
        }

        /// <summary>
        /// [Admin] Gán danh sách vai trò (Roles) cho một người dùng
        /// </summary>
        [HttpPost("users/{id:guid}/roles")]
        [HasPermission("role:manage")]
        public async Task<ActionResult<ApiResponse<bool>>> AssignRoles(Guid id, [FromBody] List<Guid> roleIds)
        {
            var command = new AssignUserRolesCommand
            {
                UserId = id,
                RoleIds = roleIds
            };

            await _mediator.Send(command);
            return Ok(ApiResponse<bool>.Ok(true, "Cập nhật vai trò cho người dùng thành công."));
        }

        #endregion

        #region --- 2. Roles & Permissions Management ---

        /// <summary>
        /// [Admin] Lấy danh sách tất cả các Role kèm danh sách Permission của Role đó
        /// </summary>
        [HttpGet("roles")]
        [HasPermission("role:manage")]
        public async Task<ActionResult<ApiResponse<List<RoleDto>>>> GetRoles()
        {
            var result = await _mediator.Send(new GetRolesQuery());
            return Ok(ApiResponse<List<RoleDto>>.Ok(result, "Lấy danh sách vai trò thành công."));
        }

        /// <summary>
        /// [Admin] Lấy danh mục tất cả Permission có trong hệ thống
        /// </summary>
        [HttpGet("permissions")]
        [HasPermission("role:manage")]
        public async Task<ActionResult<ApiResponse<List<PermissionDto>>>> GetPermissions()
        {
            var result = await _mediator.Send(new GetPermissionsQuery());
            return Ok(ApiResponse<List<PermissionDto>>.Ok(result, "Lấy danh sách quyền hạn thành công."));
        }

        #endregion
    }
}
