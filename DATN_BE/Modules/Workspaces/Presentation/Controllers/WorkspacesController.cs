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
        /// Lấy danh sách các vai trò khả dụng trong Workspace (cho dropdown mời thành viên)
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
        /// Xoá thành viên khỏi Workspace (Owner, SystemAdmin hoặc tự rời)
        /// </summary>
        [HttpDelete("{workspaceId:guid}/members/{userId:guid}")]
        public async Task<ActionResult<ApiResponse<bool>>> RemoveMember(Guid workspaceId, Guid userId)
        {
            var result = await _mediator.Send(new RemoveMemberFromWorkspaceCommand(workspaceId, userId));
            return Ok(ApiResponse<bool>.Ok(result, "Xoá thành viên khỏi Workspace thành công."));
        }
    }
}
