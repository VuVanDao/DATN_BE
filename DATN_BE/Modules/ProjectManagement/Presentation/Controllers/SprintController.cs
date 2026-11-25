using DATN_BE.Modules.ProjectManagement.Application.Commands.CompleteSprint;
using DATN_BE.Modules.ProjectManagement.Application.Commands.CreateSprint;
using DATN_BE.Modules.ProjectManagement.Application.Commands.DeleteSprint;
using DATN_BE.Modules.ProjectManagement.Application.Commands.MoveCardToSprint;
using DATN_BE.Modules.ProjectManagement.Application.Commands.StartSprint;
using DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateSprint;
using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Modules.ProjectManagement.Application.Queries.GetProjectBacklog;
using DATN_BE.Modules.ProjectManagement.Application.Queries.GetProjectSprints;
using DATN_BE.Modules.Task.Application.DTOs;
using DATN_BE.Shared.Application.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DATN_BE.Modules.ProjectManagement.Presentation.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SprintController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SprintController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Tạo mới một Sprint cho Dự án (trạng thái ban đầu: Future)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<SprintDto>>> CreateSprint([FromBody] CreateSprintCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(ApiResponse<SprintDto>.Ok(result, "Tạo mới Sprint thành công."));
        }

        /// <summary>
        /// Cập nhật thông tin Sprint (Tên, Mục tiêu, Thời gian)
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ApiResponse<SprintDto>>> UpdateSprint(Guid id, [FromBody] UpdateSprintCommand command)
        {
            command.SprintId = id;
            var result = await _mediator.Send(command);
            return Ok(ApiResponse<SprintDto>.Ok(result, "Cập nhật Sprint thành công."));
        }

        /// <summary>
        /// Xóa Sprint (các thẻ bên trong sẽ tự động trở về Backlog)
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteSprint(Guid id)
        {
            var result = await _mediator.Send(new DeleteSprintCommand(id));
            return Ok(ApiResponse<bool>.Ok(result, "Xóa Sprint thành công."));
        }

        /// <summary>
        /// Bắt đầu chạy Sprint (Chuyển từ Future sang Active)
        /// </summary>
        [HttpPost("{id:guid}/start")]
        public async Task<ActionResult<ApiResponse<SprintDto>>> StartSprint(Guid id, [FromBody] StartSprintCommand? command = null)
        {
            var cmd = command ?? new StartSprintCommand();
            cmd.SprintId = id;
            var result = await _mediator.Send(cmd);
            return Ok(ApiResponse<SprintDto>.Ok(result, "Bắt đầu Sprint thành công."));
        }

        /// <summary>
        /// Hoàn thành Sprint (Chuyển sang Closed và chuyển các thẻ dở dang)
        /// </summary>
        [HttpPost("{id:guid}/complete")]
        public async Task<ActionResult<ApiResponse<SprintDto>>> CompleteSprint(Guid id, [FromBody] CompleteSprintCommand command)
        {
            command.SprintId = id;
            var result = await _mediator.Send(command);
            return Ok(ApiResponse<SprintDto>.Ok(result, "Hoàn thành Sprint thành công."));
        }

        /// <summary>
        /// Gán Thẻ vào Sprint hoặc chuyển về Backlog
        /// </summary>
        [HttpPost("move-card")]
        public async Task<ActionResult<ApiResponse<CardDto>>> MoveCardToSprint([FromBody] MoveCardToSprintCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(ApiResponse<CardDto>.Ok(result, "Chuyển thẻ thành công."));
        }

        /// <summary>
        /// Lấy danh sách Sprint của một Dự án (hoặc theo Bảng Scrum)
        /// </summary>
        [HttpGet("project/{projectId:guid}")]
        public async Task<ActionResult<ApiResponse<List<SprintDto>>>> GetProjectSprints(
            Guid projectId,
            [FromQuery] Guid? boardId = null,
            [FromQuery] bool includeClosed = false)
        {
            var result = await _mediator.Send(new GetProjectSprintsQuery(projectId, boardId, includeClosed));
            return Ok(ApiResponse<List<SprintDto>>.Ok(result, "Lấy danh sách Sprint thành công."));
        }

        /// <summary>
        /// Lấy toàn bộ dữ liệu Backlog và Sprints chưa đóng phục vụ màn hình Backlog (hoặc theo Bảng Scrum)
        /// </summary>
        [HttpGet("project/{projectId:guid}/backlog")]
        public async Task<ActionResult<ApiResponse<ProjectBacklogDto>>> GetProjectBacklog(
            Guid projectId,
            [FromQuery] Guid? boardId = null)
        {
            var result = await _mediator.Send(new GetProjectBacklogQuery(projectId, boardId));
            return Ok(ApiResponse<ProjectBacklogDto>.Ok(result, "Lấy dữ liệu Backlog thành công."));
        }
    }
}
