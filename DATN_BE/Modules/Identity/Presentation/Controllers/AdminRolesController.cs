using DATN_BE.Modules.Identity.Application.DTOs;
using DATN_BE.Modules.Identity.Application.Queries.GetPermissions;
using DATN_BE.Modules.Identity.Application.Queries.GetRoles;
using DATN_BE.Shared.Application.Models;
using DATN_BE.Shared.Infrastructure.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DATN_BE.Modules.Identity.Presentation.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminRolesController : ControllerBase
    {
        private readonly IMediator _mediator;
        public AdminRolesController(IMediator mediator)
        {
            _mediator = mediator;
        }
        /// <summary>
        /// Lấy danh sách tất cả các Role kèm danh sách Permission của Role đó
        /// </summary>
        [HttpGet("roles")]
        [HasPermission("role:manage")]
        public async Task<ActionResult<ApiResponse<List<RoleDto>>>> GetRoles()
        {
            var result = await _mediator.Send(new GetRolesQuery());
            return Ok(ApiResponse<List<RoleDto>>.Ok(result, "Lấy danh sách vai trò thành công."));
        }
        /// <summary>
        /// Lấy danh mục tất cả Permission có trong hệ thống
        /// </summary>
        [HttpGet("permissions")]
        [HasPermission("role:manage")]
        public async Task<ActionResult<ApiResponse<List<PermissionDto>>>> GetPermissions()
        {
            var result = await _mediator.Send(new GetPermissionsQuery());
            return Ok(ApiResponse<List<PermissionDto>>.Ok(result, "Lấy danh sách quyền hạn thành công."));
        }
    }
}
