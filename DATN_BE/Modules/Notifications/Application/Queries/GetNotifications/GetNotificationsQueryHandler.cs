using DATN_BE.Modules.Notifications.Application.DTOs;
using DATN_BE.Modules.Notifications.Application.Helpers;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Notifications.Application.Queries.GetNotifications
{
    public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, NotificationListResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetNotificationsQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<NotificationListResponseDto> Handle(
            GetNotificationsQuery request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new ForbiddenException("Bạn cần đăng nhập để xem thông báo.");

            var baseQuery = _context.Notifications
                .AsNoTracking()
                .Where(n => n.ReceiverId == currentUserId);

            var unreadCount = await baseQuery.CountAsync(n => !n.IsRead, cancellationToken);

            var filteredQuery = request.UnreadOnly
                ? baseQuery.Where(n => !n.IsRead)
                : baseQuery;

            var totalCount = await filteredQuery.CountAsync(cancellationToken);

            var entities = await filteredQuery
                .Include(n => n.Sender)
                .OrderByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.Id)
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var targetUrls = await NotificationMapper.ResolveTargetUrlsAsync(_context, entities, cancellationToken);

            var items = entities
                .Select(entity => NotificationMapper.ToDto(entity, targetUrls))
                .ToList();

            return new NotificationListResponseDto
            {
                UnreadCount = unreadCount,
                TotalCount = totalCount,
                HasMore = (request.PageIndex * request.PageSize) < totalCount,
                Items = items
            };
        }
    }
}
