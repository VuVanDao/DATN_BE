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
        public AddCardCommentCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<CardCommentDto> Handle(AddCardCommentCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new ForbiddenException("Bạn cần đăng nhập để bình luận.");
            var cardExists = await _context.Cards.AnyAsync(c => c.Id == request.CardId, cancellationToken);
            if (!cardExists)
            {
                throw new NotFoundException($"Không tìm thấy card với id {request.CardId}");
            }
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
