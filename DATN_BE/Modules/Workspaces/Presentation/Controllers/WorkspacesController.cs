using DATN_BE.Modules.Workspaces.Application.Queries.GetWorkspaceProjects;
using DATN_BE.Modules.Workspaces.Application.Queries.GetWorkspaceAuditLogs;
using DATN_BE.Modules.Security.Application.DTOs;
using DATN_BE.Modules.Workspaces.Application.Queries.GetWorkspaceMembers;
using DATN_BE.Modules.Workspaces.Application.Commands.AddWorkspaceMember;
using DATN_BE.Modules.Workspaces.Application.Commands.CreateWorkspace;
using DATN_BE.Modules.Workspaces.Application.Commands.DeleteWorkspace;
using DATN_BE.Modules.Workspaces.Application.Commands.RemoveMemberFromProject;
using DATN_BE.Modules.Workspaces.Application.Commands.UpdateWorkspace;
using DATN_BE.Modules.Workspaces.Application.DTOs;
using DATN_BE.Modules.Workspaces.Application.Queries.GetMemberRemovalImpact;
using DATN_BE.Modules.Workspaces.Application.Queries.GetUserWorkspaces;
using DATN_BE.Modules.Workspaces.Application.Queries.GetWorkspaceBySlug;
using DATN_BE.Modules.Workspaces.Application.Queries.GetWorkspaceRoles;
using DATN_BE.Shared.Application.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DATN_BE.Modules.Workspaces.Presentation.Controllers
{ 
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class WorkspacesController : ControllerBase
    {
        private readonly IMediator _mediator;
        public WorkspacesController(IMediator mediator)
        {
            _mediator = mediator;
        }
        /// <summary>
        /// Tạo Không gian làm việc mới
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<WorkspaceDto>>> CreateWorkspace([FromBody] CreateWorkspaceCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(ApiResponse<WorkspaceDto>.Ok(result,"Tạo mới workspace thành công"));
        }
        /// <summary>
        /// Lấy danh sách Workspace của tôi
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<WorkspaceDto>>>> GetMyWorkspaces()
        {
            var result = await _mediator.Send(new GetUserWorkspacesQuery());
            return Ok(ApiResponse<List<WorkspaceDto>>.Ok(result, "Lấy danh sách Workspace thành công."));
        }

        /// <summary>
        /// Lấy danh sách Dự án trong Workspace
        /// </summary>
        [HttpGet("{workspaceId:guid}/projects")]
        public async Task<ActionResult<ApiResponse<PagedResult<WorkspaceProjectDto>>>> GetWorkspaceProjects(
            Guid workspaceId, CancellationToken cancellationToken, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _mediator.Send(new GetWorkspaceProjectsQuery(workspaceId, pageIndex, pageSize), cancellationToken);
            return Ok(ApiResponse<PagedResult<WorkspaceProjectDto>>.Ok(result, "Lấy danh sách dự án của workspace thành công"));
        }

        /// <summary>
        /// Xem lịch sử quản trị Workspace (chỉ WorkspaceOwner), kể cả Workspace đã xoá mềm.
        /// </summary>
        [HttpGet("{id:guid}/audit-logs")]
        public async Task<ActionResult<ApiResponse<PagedResult<ActivityLogDto>>>> GetAuditLogs(
            Guid id, CancellationToken cancellationToken, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _mediator.Send(new GetWorkspaceAuditLogsQuery(id, pageIndex, pageSize), cancellationToken);
            return Ok(ApiResponse<PagedResult<ActivityLogDto>>.Ok(result, "Lấy lịch sử Workspace thành công."));
        }

        /// <summary>
        /// Lấy danh sách thành viên Workspace theo trang.
        /// </summary>
        [HttpGet("{id:guid}/members")]
        public async Task<ActionResult<ApiResponse<PagedResult<WorkspaceMemberDto>>>> GetMembers(
            Guid id, CancellationToken cancellationToken, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _mediator.Send(new GetWorkspaceMembersQuery(id, pageIndex, pageSize), cancellationToken);
            return Ok(ApiResponse<PagedResult<WorkspaceMemberDto>>.Ok(result, "Lấy danh sách thành viên thành công."));
        }

        /// <summary>
        /// Lấy danh sách các vai trò khả dụng trong Workspace (cho dropdown mời thành viên).
        /// </summary>
        [HttpGet("roles")]
        public async Task<ActionResult<ApiResponse<List<RoleDto>>>> GetWorkspaceRoles()
        {
            var result = await _mediator.Send(new GetWorkspaceRolesQuery());
            return Ok(ApiResponse<List<RoleDto>>.Ok(result, "Lấy danh sách vai trò thành công."));
        }

        /// <summary>
        /// Thêm nhiều thành viên vào Workspace 
        /// </summary>
        [HttpPost("{id:guid}/members")]
        public async Task<ActionResult<ApiResponse<AddWorkspaceMembersResultDto>>> AddMembers(
            Guid id,
            [FromBody] AddMemberToWorkspaceCommand command)
        {
            command.WorkspaceId = id;
            var result = await _mediator.Send(command);
            var message = result.TotalFailed == 0
                ? $"Đã thêm thành công {result.TotalSuccess} thành viên."
                : $"Thêm thành công {result.TotalSuccess}/{result.TotalRequested} thành viên.";
            return Ok(ApiResponse<AddWorkspaceMembersResultDto>.Ok(result, message));
        }

        /// <summary>
        /// Lấy chi tiết Workspace theo Slug
        /// </summary>
        [HttpGet("{slug}")]
        public async Task<ActionResult<ApiResponse<WorkspaceDetailDto>>> GetWorkspaceBySlug(string slug)
        {
            var result = await _mediator.Send(new GetWorkspaceBySlugQuery { Slug = slug });
            return Ok(ApiResponse<WorkspaceDetailDto>.Ok(result, "Lấy chi tiết Workspace thành công."));
        }
        /// <summary>
        /// Cập nhật thông tin Workspace (tên, mô tả, logo, ảnh bìa)
        /// </summary>
        [HttpPut("{id:guid}")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponse<WorkspaceDetailDto>>> UpdateWorkspace(
            Guid id, [FromForm] UpdateWorkspaceCommand command)
        {
            command.WorkspaceId = id;
            var result = await _mediator.Send(command);
            return Ok(ApiResponse<WorkspaceDetailDto>.Ok(result, "Cập nhật Workspace thành công."));
        }

        /// <summary>
        /// Xóa Workspace (Chỉ SystemAdmin hoặc WorkspaceOwner)
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteWorkspace(Guid id)
        {
            var result = await _mediator.Send(new DeleteWorkspaceCommand(id));
            return Ok(ApiResponse<bool>.Ok(result, "Xóa Workspace thành công."));
        }
        /// <summary>
        /// Xem ảnh hưởng trước khi xoá thành viên khỏi Workspace (đang làm Trưởng dự án nào, đang được gán bao nhiêu card)
        /// </summary>
        [HttpGet("{id:guid}/members/{userId:guid}/removal-impact")]
        public async Task<ActionResult<ApiResponse<MemberRemovalImpactDto>>> GetMemberRemovalImpact(Guid id, Guid userId)
        {
            var result = await _mediator.Send(new GetMemberRemovalImpactQuery
            {
                WorkspaceId = id,
                MemberId = userId
            });
            return Ok(ApiResponse<MemberRemovalImpactDto>.Ok(result, "Lấy thông tin ảnh hưởng thành công."));
        }

        /// <summary>
        /// Xoá thành viên khỏi Workspace (Owner hoặc tự rời)
        /// </summary>
        [HttpDelete("{workspaceId:guid}/members/{userId:guid}")]
        public async Task<ActionResult<ApiResponse<string>>> RemoveMember(Guid workspaceId, Guid userId)
        {
            var result = await _mediator.Send(new RemoveMemberFromWorkspaceCommand(workspaceId, userId));
            return Ok(ApiResponse<string>.Ok(result));
        }
    }
}
