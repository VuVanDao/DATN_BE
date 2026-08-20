using DATN_BE.Modules.Workspaces.Application.Commands.AddWorkspaceMember;
using DATN_BE.Modules.Workspaces.Application.Commands.CreateWorkspace;
using DATN_BE.Modules.Workspaces.Application.DTOs;
using DATN_BE.Modules.Workspaces.Application.Queries.GetUserWorkspaces;
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
        /// Thêm thành viên vào Workspace
        /// </summary>
        [HttpPost("{id:guid}/members")]
        public async Task<ActionResult<ApiResponse<WorkspaceMemberDto>>> AddMember(Guid id, [FromBody] AddWorkspaceMemberCommand command)
        {
            command.WorkspaceId = id;
            var result = await _mediator.Send(command);
            return Ok(ApiResponse<WorkspaceMemberDto>.Ok(result, "Thêm thành viên vào Workspace thành công."));
        }
    }
}
