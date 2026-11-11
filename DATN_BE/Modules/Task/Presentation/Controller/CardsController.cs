using DATN_BE.Modules.Task.Application.Commands.AddCardComment;
using DATN_BE.Modules.Task.Application.Commands.AddCardDependency;
using DATN_BE.Modules.Task.Application.Commands.CreateCard;
using DATN_BE.Modules.Task.Application.Commands.DeleteCard;
using DATN_BE.Modules.Task.Application.Commands.MoveCard;
using DATN_BE.Modules.Task.Application.Commands.RemoveCardDependency;
using DATN_BE.Modules.Task.Application.Commands.UpdateCard;
using DATN_BE.Modules.Task.Application.DTOs;
using DATN_BE.Modules.Task.Application.Queries.GetCardDependencies;
using DATN_BE.Modules.Task.Application.Queries.GetCardDetail;
using DATN_BE.Modules.Task.Application.Queries.GetProjectBacklogCards;
using DATN_BE.Modules.Task.Application.Queries.SearchCardCandidates;
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
        public async Task<ActionResult<ApiResponse<CardDetailDto>>> GetCardDetail(Guid cardId)
        {
            var result = await _mediator.Send(new GetCardDetailQuery(cardId));
            return Ok(ApiResponse<CardDetailDto>.Ok(result, "Lấy chi tiết card thành công"));
        }
        /// <summary>
        /// Cập nhật thông tin Thẻ (hỗ trợ FormData và upload file ảnh bìa)
        /// </summary>
        [HttpPut("{cardId}")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponse<CardDto>>> UpdateCard(Guid cardId, [FromForm] UpdateCardCommand command)
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
        /// <summary>
        /// Xoá 1 thẻ
        /// </summary>
        [HttpDelete("{cardId}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteCard(Guid cardId)
        {
            var result = await _mediator.Send(new DeleteCardCommand(cardId));
            return Ok(ApiResponse<bool>.Ok(result, "Xoá card thành công"));
        }

        /// <summary>
        /// Tạo Thẻ công việc mới trong Backlog hoặc Sprint của Dự án (Tự động gán vào Cột đầu tiên của Bảng Scrum)
        /// </summary>
        [HttpPost("backlog")]
        public async Task<ActionResult<ApiResponse<CardDto>>> CreateBacklogCard([FromBody] DATN_BE.Modules.Task.Application.Commands.CreateBacklogCard.CreateBacklogCardCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(ApiResponse<CardDto>.Ok(result, "Tạo công việc trong Backlog thành công."));
        }

        /// <summary>
        /// Lấy danh sách thẻ trong kho Product Backlog của Dự án (những thẻ có SprintId == null)
        /// </summary>
        [HttpGet("project/{projectId:guid}/backlog")]
        public async Task<ActionResult<ApiResponse<List<CardDto>>>> GetProjectBacklogCards(
            Guid projectId,
            [FromQuery] Guid? boardId = null)
        {
            var result = await _mediator.Send(new GetProjectBacklogCardsQuery(projectId, boardId));
            return Ok(ApiResponse<List<CardDto>>.Ok(result, "Lấy danh sách thẻ Backlog thành công."));
        }

        /// <summary>
        /// Thêm liên kết phụ thuộc giữa 2 thẻ (BlockedBy hoặc Blocking)
        /// </summary>
        [HttpPost("{cardId:guid}/dependencies")]
        public async Task<ActionResult<ApiResponse<CardDependencyItemDto>>> AddDependency(
            Guid cardId,
            [FromBody] AddCardDependencyRequest request)
        {
            var command = new AddCardDependencyCommand
            {
                CardId = cardId,
                TargetCardId = request.TargetCardId,
                Direction = request.Direction
            };
            var result = await _mediator.Send(command);
            return Ok(ApiResponse<CardDependencyItemDto>.Ok(result, "Thêm liên kết phụ thuộc thành công."));
        }

        /// <summary>
        /// Xóa liên kết phụ thuộc giữa 2 thẻ theo Id của quan hệ phụ thuộc
        /// </summary>
        [HttpDelete("dependencies/{dependencyId:guid}")]
        public async Task<ActionResult<ApiResponse<bool>>> RemoveDependency(Guid dependencyId)
        {
            var result = await _mediator.Send(new RemoveCardDependencyCommand(dependencyId));
            return Ok(ApiResponse<bool>.Ok(result, "Xóa liên kết phụ thuộc thành công."));
        }

        /// <summary>
        /// Lấy danh sách các liên kết phụ thuộc của thẻ (thẻ đang bị chặn bởi ai và đang chặn ai)
        /// </summary>
        [HttpGet("{cardId:guid}/dependencies")]
        public async Task<ActionResult<ApiResponse<CardDependenciesResultDto>>> GetDependencies(Guid cardId)
        {
            var result = await _mediator.Send(new GetCardDependenciesQuery(cardId));
            return Ok(ApiResponse<CardDependenciesResultDto>.Ok(result, "Lấy danh sách phụ thuộc thành công."));
        }

        /// <summary>
        /// Tìm kiếm các thẻ ứng viên trong cùng Dự án để thiết lập liên kết phụ thuộc
        /// </summary>
        [HttpGet("{cardId:guid}/dependency-candidates")]
        public async Task<ActionResult<ApiResponse<List<CardCandidateDto>>>> SearchCandidates(
            Guid cardId,
            [FromQuery] string? query = null)
        {
            var result = await _mediator.Send(new SearchCardCandidatesQuery(cardId, query));
            return Ok(ApiResponse<List<CardCandidateDto>>.Ok(result, "Lấy danh sách gợi ý liên kết thành công."));
        }
    }
}
