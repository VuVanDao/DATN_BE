using DATN_BE.Modules.Security.Domain.Enum;
using MediatR;

namespace DATN_BE.Shared.Application.Events
{
    public class EntityActivityLoggedEvent : INotification
    {
        public Guid UserId { get; }
        public ActivityEntityType EntityType { get; }
        public Guid EntityId { get; }
        public ActivityActionType ActionType { get; }
        public object? Details { get; }

        public EntityActivityLoggedEvent(
            Guid userId,
            ActivityEntityType entityType,
            Guid entityId,
            ActivityActionType actionType,
            object? details = null)
        {
            UserId = userId;
            EntityType = entityType;
            EntityId = entityId;
            ActionType = actionType;
            Details = details;
        }
    }
}
