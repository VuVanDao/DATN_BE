using System.Text.Json;
using DATN_BE.Modules.Security.Domain.Entities;
using DATN_BE.Shared.Application.Events;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DATN_BE.Modules.Security.Application.EventHandlers
{
    public class EntityActivityLoggedEventHandler : INotificationHandler<EntityActivityLoggedEvent>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<EntityActivityLoggedEventHandler>? _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
        };

        public EntityActivityLoggedEventHandler(
            IApplicationDbContext context,
            ILogger<EntityActivityLoggedEventHandler>? logger = null)
        {
            _context = context;
            _logger = logger;
        }

        public async System.Threading.Tasks.Task Handle(EntityActivityLoggedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                string? detailsJson = null;
                if (notification.Details != null)
                {
                    detailsJson = notification.Details is string rawString
                        ? rawString
                        : JsonSerializer.Serialize(notification.Details, JsonOptions);
                }

                var activityLog = new ActivityLog
                {
                    Id = Guid.NewGuid(),
                    UserId = notification.UserId,
                    EntityType = notification.EntityType,
                    EntityId = notification.EntityId,
                    ActionType = notification.ActionType,
                    DetailsJson = detailsJson
                };

                _context.ActivityLogs.Add(activityLog);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex,
                    "Lỗi khi lưu ActivityLog cho EntityType {EntityType}, EntityId {EntityId}, ActionType {ActionType}",
                    notification.EntityType, notification.EntityId, notification.ActionType);
            }
        }
    }
}
