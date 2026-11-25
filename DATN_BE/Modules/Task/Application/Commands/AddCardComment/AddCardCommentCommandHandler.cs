using DATN_BE.Modules.ProjectManagement.Presentation.Hubs;
using DATN_BE.Modules.Security.Domain.Enum;
using DATN_BE.Modules.Task.Application.DTOs;
using DATN_BE.Modules.Task.Domain.Entities;
using DATN_BE.Shared.Application.Events;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DATN_BE.Modules.Task.Application.Commands.AddCardComment
{
    public class AddCardCommentCommandHandler : IRequestHandler<AddCardCommentCommand, CardCommentDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IProjectAuthorizationService _authorizationService;
        private readonly IHubContext<BoardHub>? _hubContext;
        private readonly ILogger<AddCardCommentCommandHandler>? _logger;
        private readonly IMediator? _mediator;

        public AddCardCommentCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IProjectAuthorizationService authorizationService,
            IHubContext<BoardHub>? hubContext = null,
            ILogger<AddCardCommentCommandHandler>? logger = null,
            IMediator? mediator = null)
        {
            _context = context;
            _currentUserService = currentUserService;
            _authorizationService = authorizationService;
            _hubContext = hubContext;
            _logger = logger;
            _mediator = mediator;
        }

        public async Task<CardCommentDto> Handle(AddCardCommentCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new UnauthorizedException("Bạn cần đăng nhập để bình luận.");
            var card = await _context.Cards.Include(c => c.Column).ThenInclude(c => c.Board)
                .FirstOrDefaultAsync(c => c.Id == request.CardId, cancellationToken)
                ?? throw new NotFoundException($"Không tìm thấy card với id {request.CardId}");
            await _authorizationService.EnsureCanModifyProjectAsync(card.Column.Board.ProjectId, cancellationToken);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken)
                ?? throw new NotFoundException("Người dùng không tồn tại.");
            var comment = new CardComment
            {
                CardId = request.CardId,
                UserId = currentUserId,
                ContentText = request.ContentText.Trim()
            };
            _context.CardComments.Add(comment);
            await _context.SaveChangesAsync(cancellationToken);

            if (_mediator != null)
            {
                await _mediator.Publish(new EntityActivityLoggedEvent(
                    currentUserId,
                    ActivityEntityType.Card,
                    card.Id,
                    ActivityActionType.Commented,
                    new
                    {
                        cardTitle = card.Title,
                        commentId = comment.Id,
                        preview = comment.ContentText.Length > 80 ? comment.ContentText[..80] + "..." : comment.ContentText
                    }), cancellationToken);
            }

            var result = new CardCommentDto
            {
                Id = comment.Id,
                CardId = comment.CardId,
                UserId = comment.UserId,
                Username = user.Username,
                UserAvatarUrl = user.AvatarUrl,
                ContentText = comment.ContentText,
                CreatedAt = comment.CreatedAt
            };

            if (_hubContext != null)
            {
                try
                {
                    var boardId = card.Column.BoardId;
                    await _hubContext.Clients
                        .Group(BoardHub.GroupName(boardId))
                        .SendAsync(
                            BoardHub.CardCommentAdded,
                            new CardCommentAddedMessage(
                                boardId,
                                result,
                                currentUserId,
                                DateTimeOffset.UtcNow),
                            CancellationToken.None);
                }
                catch (Exception exception)
                {
                    _logger?.LogWarning(exception, "Không thể phát sự kiện CardCommentAdded cho board {BoardId}.", card.Column.BoardId);
                }
            }

            return result;
        }
    }
}
