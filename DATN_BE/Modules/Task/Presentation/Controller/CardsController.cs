using DATN_BE.Modules.Task.Application.Commands.AddCardComment;
using DATN_BE.Modules.Task.Application.Commands.CreateCard;
using DATN_BE.Modules.Task.Application.Commands.MoveCard;
using DATN_BE.Modules.Task.Application.Commands.UpdateCard;
using DATN_BE.Modules.Task.Application.DTOs;
using DATN_BE.Modules.Task.Application.Queries.GetCardById;
using DATN_BE.Shared.Application.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DATN_BE.Modules.Task.Presentation.Controller
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CardsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public CardsController(IMediator mediator)
        {
            _mediator = mediator;
        }
        /// <summary>
        /// Tạo Thẻ công việc mới
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<CardDto>>> CreateCard([FromBody] CreateCardCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(ApiResponse<CardDto>.Ok(result, "Tạo card mới thành công"));
        }
        /// <summary>
        /// Lấy chi tiết Thẻ kèm danh sách bình luận
        /// </summary>
        [HttpGet("{cardId}")]
        public async Task<ActionResult<ApiResponse<CardDetailDto>>> GetCardById(Guid cardId)
        {
            var result = await _mediator.Send(new GetCardByIdQuery(cardId));
            return Ok(ApiResponse<CardDetailDto>.Ok(result, "Lấy chi tiết card thành công"));
        }
        /// <summary>
        /// Cập nhật thông tin Thẻ
        /// </summary>
        [HttpPut("{cardId}")]
        public async Task<ActionResult<ApiResponse<CardDto>>> UpdateCard(Guid cardId, [FromBody] UpdateCardCommand command)
        {
            command.CardId = cardId;
            var result = await _mediator.Send(command);
            return Ok(ApiResponse<CardDto>.Ok(result, "Cập nhật card thành công"));
        }
        /// <summary>
        /// Kéo thả Thẻ (Đổi vị trí trong cột hoặc sang cột khác)
        /// </summary>
        [HttpPost("{cardId}/move")]
        public async Task<ActionResult<ApiResponse<bool>>> MoveCard(Guid cardId, [FromBody] MoveCardCommand command)
        {
            command.CardId = cardId;
            var result = await _mediator.Send(command);
            return Ok(ApiResponse<bool>.Ok(result, "Di chuyển card thành công"));
        }
        /// <summary>
        /// Thêm bình luận vào Thẻ
        /// </summary>
        [HttpPost("{id:guid}/comments")]
        public async Task<ActionResult<ApiResponse<CardCommentDto>>> AddComment(Guid id, [FromBody] AddCardCommentCommand command)
        {
            command.CardId = id;
            var result = await _mediator.Send(command);
            return Ok(ApiResponse<CardCommentDto>.Ok(result, "Bình luận thành công."));
        }
    }
}
