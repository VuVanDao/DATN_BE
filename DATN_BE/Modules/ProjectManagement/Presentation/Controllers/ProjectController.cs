using DATN_BE.Modules.ProjectManagement.Application.Commands.CreateColumn;
using DATN_BE.Modules.ProjectManagement.Application.Commands.CreateProject;
using DATN_BE.Modules.ProjectManagement.Application.DTOs;
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
        [HttpPost("project")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> CreateProject([FromBody] CreateProjectCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(ApiResponse<ProjectDto>.Ok(result,"Tạo mới 1 project mới thành công"));
        }
        /// <summary>
        /// Lấy danh sách Dự án trong Workspace
        /// </summary>
        [HttpGet("workspace-project/{workspaceId:guid}")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> GetWorkspaceProjects(Guid workspaceId)
        {
            var result = await _mediator.Send(new GetWorkspaceProjectsQuery(workspaceId));
            return Ok(ApiResponse<List<ProjectDto>>.Ok(result, "Lấy danh sách Dự án thành công."));
        }

    }
}
