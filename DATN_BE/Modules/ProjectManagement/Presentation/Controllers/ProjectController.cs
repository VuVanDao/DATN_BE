using DATN_BE.Modules.ProjectManagement.Application.Commands.AddMemberToProejct;
using DATN_BE.Modules.ProjectManagement.Application.Commands.CreateColumn;
using DATN_BE.Modules.ProjectManagement.Application.Commands.CreateProject;
using DATN_BE.Modules.ProjectManagement.Application.Commands.RemoveMemberFromProject;
using DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateProjectLeader;
using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Modules.ProjectManagement.Application.Queries.GetProjectMember;
using DATN_BE.Modules.ProjectManagement.Application.Queries.GetProjectDetail;
using DATN_BE.Modules.ProjectManagement.Application.Queries.GetWorkspaceProjects;
using DATN_BE.Shared.Application.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        /// Lấy chi tiết một Dự án theo projectKey.
        /// </summary>
        [HttpGet("{projectKey}")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> GetProjectDetail(string projectKey)
        {
            var result = await _mediator.Send(new GetProjectDetailQuery(projectKey));
            return Ok(ApiResponse<ProjectDto>.Ok(result, "Lấy thông tin Dự án thành công."));
        }

        /// <summary>
        /// Lấy danh sách Dự án trong Workspace
        /// </summary>
        /// bên FE chưua dùng
        [HttpGet("workspace-project/{workspaceId:guid}")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> GetWorkspaceProjects(Guid workspaceId)
        {
            var result = await _mediator.Send(new GetWorkspaceProjectsQuery(workspaceId));
            return Ok(ApiResponse<List<ProjectDto>>.Ok(result, "Lấy danh sách Dự án thành công."));
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
            return Ok(ApiResponse<bool>.Ok(result, "Xóa thành viên khỏi dự án thành công."));
        }
    }
}
