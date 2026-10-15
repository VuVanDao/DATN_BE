using DATN_BE.Modules.ProjectManagement.Application.Commands.CreateBoard;
using DATN_BE.Modules.ProjectManagement.Application.Commands.CreateColumn;
using DATN_BE.Modules.ProjectManagement.Application.Commands.DeleteBoard;
using DATN_BE.Modules.ProjectManagement.Application.Commands.ReorderColumns;
using DATN_BE.Modules.ProjectManagement.Application.Commands.UpdateBoard;
using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Modules.ProjectManagement.Application.Queries.GetBoardDetail;
using DATN_BE.Modules.ProjectManagement.Application.Queries.GetProjectBoards;
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
        /// Tạo mới Bảng trong một Dự án. Chỉ Trưởng dự án được phép thực hiện.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<BoardDto>>> CreateBoard([FromBody] CreateBoardCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(ApiResponse<BoardDto>.Ok(result, "Tạo mới Bảng thành công."));
        }
        /// <summary>
        /// Cập nhật tiêu đề, slug và loại Bảng. Chỉ Trưởng dự án được phép thực hiện.
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ApiResponse<BoardDto>>> UpdateBoard(Guid id, [FromBody] UpdateBoardCommand command)
        {
            command.BoardId = id;
            var result = await _mediator.Send(command);
            return Ok(ApiResponse<BoardDto>.Ok(result, "Cập nhật Bảng thành công."));
        }
        /// <summary>
        /// Xoá mềm Bảng và toàn bộ Cột, Thẻ thuộc Bảng. Chỉ Trưởng dự án được phép thực hiện.
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteBoard(Guid id)
        {
            var result = await _mediator.Send(new DeleteBoardCommand { BoardId = id });
            return Ok(ApiResponse<bool>.Ok(result, "Đã xoá Bảng và các dữ liệu trực thuộc."));
        }
        /// <summary>
        /// Lấy danh sách Bảng trong Dự án. Chỉ thành viên của Dự án được phép xem.
        /// </summary>
        [HttpGet("project/{projectId:guid}")]
        public async Task<ActionResult<ApiResponse<List<BoardDto>>>> GetProjectBoards(Guid projectId)
        {
            var result = await _mediator.Send(new GetProjectBoardsQuery(projectId));
            return Ok(ApiResponse<List<BoardDto>>.Ok(result, "Lấy danh sách Bảng thành công."));
        }
        /// <summary>
        /// Lấy chi tiết Bảng kèm danh sách Cột theo thứ tự
        /// </summary>
        [HttpGet("{slug}/project/{projectId:guid}")]
        public async Task<ActionResult<ApiResponse<BoardDetailDto>>> GetBoardDetail(string slug, Guid projectId)
        {
            var result = await _mediator.Send(new GetBoardDetailQuery(slug,projectId));
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
