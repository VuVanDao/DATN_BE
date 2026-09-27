using DATN_BE.Modules.Task.Application.DTOs;
using DATN_BE.Modules.Task.Domain.Entities;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Task.Application.Commands.AddCardComment
{
    public class AddCardCommentCommandHandler : IRequestHandler<AddCardCommentCommand, CardCommentDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IProjectAuthorizationService _authorizationService;
        public AddCardCommentCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IProjectAuthorizationService authorizationService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _authorizationService = authorizationService;
        }

        public async Task<CardCommentDto> Handle(AddCardCommentCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new ForbiddenException("Bạn cần đăng nhập để bình luận.");
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
            return new CardCommentDto
            {
                Id = comment.Id,
                CardId = comment.CardId,
                UserId = comment.UserId,
                Username = user.Username,
                UserAvatarUrl = user.AvatarUrl,
                ContentText = comment.ContentText,
                CreatedAt = comment.CreatedAt
            };
        }
    }
}
