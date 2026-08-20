using DATN_BE.Modules.ProjectManagement.Application.Commands.CreateColumn;
using DATN_BE.Modules.ProjectManagement.Application.Commands.ReorderColumns;
using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Modules.ProjectManagement.Application.Queries.GetBoardDetail;
using DATN_BE.Shared.Application.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DATN_BE.Modules.ProjectManagement.Presentation.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BoardController : ControllerBase
    {
        private readonly IMediator _mediator;
        public BoardController(IMediator mediator)
        {
            _mediator = mediator;
        }
        /// <summary>
        /// Lấy chi tiết Bảng kèm danh sách Cột theo thứ tự
        /// </summary>
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<BoardDetailDto>>> GetBoardDetail(Guid id)
        {
            var result = await _mediator.Send(new GetBoardDetailQuery(id));
            return Ok(ApiResponse<BoardDetailDto>.Ok(result, "Lấy thông tin Bảng thành công."));
        }
        /// <summary>
        /// Tạo mới column
        /// </summary>
        [HttpPost("{id:guid}/columns")]
        public async Task<ActionResult<ApiResponse<ColumnDto>>> CreateColumn(Guid id, [FromBody] CreateColumnCommand command)
        {
            command.BoardId = id;
            var result = await _mediator.Send(command);
            return Ok(ApiResponse<ColumnDto>.Ok(result, "Tạo mới cột thành công"));
        }
        /// <summary>
        /// Sắp xếp lại thứ tự các Cột khi kéo thả
        /// </summary>
        [HttpPut("{id:guid}/columns/reorder")]
        public async Task<ActionResult<ApiResponse<bool>>> ReorderColumns(Guid id, [FromBody] ReorderColumnsCommand command)
        {
            command.BoardId = id;
            var result = await _mediator.Send(command);
            return Ok(ApiResponse<bool>.Ok(result, "Cập nhật vị trí Cột thành công."));
        }
    }
}
