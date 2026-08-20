using DATN_BE.Modules.ProjectManagement.Application.Commands.CreateProject;
using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Shared.Application.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DATN_BE.Modules.ProjectManagement.Presentation.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectManagementController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ProjectManagementController(IMediator mediator)
        {
            _mediator = mediator;
        }
        public async Task<ActionResult<ApiResponse<ProjectDto>>> CreateProject([FromBody] CreateProjectCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(ApiResponse<ProjectDto>.Ok(result,"Tạo mới 1 project mới thành công"));
        }
    }
}
