using DATN_BE.Modules.ProjectManagement.Application.Commands.AddMemberToProejct;
using DATN_BE.Modules.ProjectManagement.Application.Commands.CreateColumn;
using DATN_BE.Modules.ProjectManagement.Application.Commands.CreateProject;
using DATN_BE.Modules.ProjectManagement.Application.Commands.DeleteProject;
using DATN_BE.Modules.ProjectManagement.Application.Commands.RemoveMemberFromProject;
using DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateProject;
using DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateProjectLeader;
using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Modules.ProjectManagement.Application.Queries.GetMyProjects;
using DATN_BE.Modules.ProjectManagement.Application.Queries.GetProjectMember;
using DATN_BE.Modules.ProjectManagement.Application.Queries.GetProjectDetail;
using DATN_BE.Shared.Application.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DATN_BE.Modules.ProjectManagement.Application.Queries.GetProjectRoles;

namespace DATN_BE.Modules.ProjectManagement.Presentation.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ProjectController(IMediator mediator)
        {
            _mediator = mediator;
        }
        /// <summary>
        /// Tạo Dự án mới trong Workspace
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> CreateProject([FromBody] CreateProjectCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(ApiResponse<ProjectDto>.Ok(result, "Tạo mới 1 project mới thành công"));
        }

        /// <summary>
        /// Cập nhật tên, mã viết tắt và mô tả dự án (WorkspaceOwner hoặc ProjectLeader).
        /// </summary>
        [HttpPut("{projectId:guid}")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> UpdateProject(
            Guid projectId,
            [FromBody] UpdateProjectCommand command,
            CancellationToken cancellationToken)
        {
            command.ProjectId = projectId;
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(ApiResponse<ProjectDto>.Ok(result, "Cập nhật dự án thành công."));
        }

        /// <summary>
        /// Xoá mềm dự án và toàn bộ Bảng, Cột, Thẻ thuộc dự án (WorkspaceOwner hoặc ProjectLeader).
        /// </summary>
        [HttpDelete("{projectId:guid}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteProject(
            Guid projectId,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new DeleteProjectCommand { ProjectId = projectId }, cancellationToken);
            return Ok(ApiResponse<bool>.Ok(result, "Đã xoá dự án và toàn bộ Bảng, Cột, Thẻ thuộc dự án."));
        }

        /// <summary>
        /// Lấy chi tiết một Dự án theo projectKey.
        /// </summary>
        [HttpGet("{projectKey}/workspace/{workspaceId}")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> GetProjectDetail(string projectKey, Guid workspaceId)
        {
            var result = await _mediator.Send(new GetProjectDetailQuery(projectKey, workspaceId));
            return Ok(ApiResponse<ProjectDto>.Ok(result, "Lấy thông tin Dự án thành công."));
        }

        /// <summary>
        /// Lấy các dự án người dùng hiện tại đã tham gia, nhóm theo Workspace.
        /// </summary>
        /// <remarks>
        /// workspaceId là bộ lọc tùy chọn. Khi truyền, kết quả có tối đa một nhóm Workspace.
        /// Không có dự án phù hợp trả về danh sách rỗng. Chỉ tính thành viên trực tiếp của dự án.
        /// </remarks>
        [HttpGet("my-projects")]
        public async Task<ActionResult<ApiResponse<List<MyProjectsWorkspaceDto>>>> GetMyProjects(
            [FromQuery] Guid? workspaceId,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMyProjectsQuery(workspaceId), cancellationToken);
            return Ok(ApiResponse<List<MyProjectsWorkspaceDto>>.Ok(result, "Lấy danh sách dự án đã tham gia thành công."));
        }

        /// <summary>
        /// Lấy danh sách thành viên trong Dự án
        /// </summary>
        [HttpGet("{projectId:guid}/members")]
        public async Task<ActionResult<ApiResponse<List<ProjectMemberDto>>>> GetProjectMembers(Guid projectId)
        {
            var result = await _mediator.Send(new GetProjectMemberQuery(projectId));
            return Ok(ApiResponse<List<ProjectMemberDto>>.Ok(result, "Lấy danh sách thành viên dự án thành công."));
        }
        /// <summary>
        /// Thêm thành viên vào Dự án
        /// </summary>
        [HttpPost("{projectId:guid}/members")]
        public async Task<ActionResult<ApiResponse<List<ProjectMemberDto>>>> AddProjectMembers(
            Guid projectId,
            [FromBody] AddMemberToProejctCommand command)
        {
            command.ProjectId = projectId;
            var result = await _mediator.Send(command);
            return Ok(ApiResponse<List<ProjectMemberDto>>.Ok(result, "Thêm thành viên vào dự án thành công."));
        }
        /// <summary>
        /// Cập nhật vai trò thành viên trong Dự án (Chuyển giao Leader / Đổi role)
        /// </summary>
        [HttpPut("{projectId:guid}/members/{userId:guid}/role")]
        public async Task<ActionResult<ApiResponse<ProjectMemberDto>>> UpdateProjectMemberRole(
            Guid projectId,
            Guid userId,
            [FromBody] UpdateProjectMemberRoleCommand command)
        {
            command.ProjectId = projectId;
            command.UserId = userId;
            var result = await _mediator.Send(command);
            return Ok(ApiResponse<ProjectMemberDto>.Ok(result, "Cập nhật vai trò thành viên thành công."));
        }
        /// <summary>
        /// Xóa thành viên khỏi Dự án
        /// </summary>
        [HttpDelete("{projectId:guid}/members/{userId:guid}")]
        public async Task<ActionResult<ApiResponse<bool>>> RemoveProjectMember(Guid projectId, Guid userId)
        {
            var result = await _mediator.Send(new RemoveMemberFromProjectCommand(projectId, userId));
            return Ok(ApiResponse<bool>.Ok(result, "Thành viên đã không còn trong dự án"));
        }
        /// <summary>
        /// lấy danh sách các quyền trong phạm vi project
        /// </summary>
        [HttpGet("roles")]
        public async Task<ActionResult<ApiResponse<List<ProjectRoleDto>>>> GetProjectRole()
        {
            var result = await _mediator.Send(new GetProjectRoleQuery());
            return Ok(ApiResponse<List<ProjectRoleDto>>.Ok(result, "Lấy danh sách các vai trò phạm vi dự án thành công"));
        }
    }
}
