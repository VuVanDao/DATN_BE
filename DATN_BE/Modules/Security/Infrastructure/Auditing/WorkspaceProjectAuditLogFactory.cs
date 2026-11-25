using System.Text.Json;
using DATN_BE.Modules.ProjectManagement.Domain.Entities;
using DATN_BE.Modules.Security.Domain.Entities;
using DATN_BE.Modules.Security.Domain.Enum;
using DATN_BE.Modules.Workspaces.Domain.Entities;
using DATN_BE.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace DATN_BE.Modules.Security.Infrastructure.Auditing;

internal static class WorkspaceProjectAuditLogFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
    };

    public static ActivityLog? Create(EntityEntry entry, Guid actorId, DateTimeOffset now)
    {
        if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            return null;

        (ActivityEntityType Type, Guid Id, Guid? MemberId)? target = entry.Entity switch
        {
            Workspace workspace => (ActivityEntityType.Workspace, workspace.Id, (Guid?)null),
            Project project => (ActivityEntityType.Project, project.Id, (Guid?)null),
            WorkspaceMember member => (ActivityEntityType.Workspace, member.WorkspaceId, (Guid?)member.UserId),
            ProjectMember member => (ActivityEntityType.Project, member.ProjectId, (Guid?)member.UserId),
            _ => ((ActivityEntityType Type, Guid Id, Guid? MemberId)?)null
        };
        if (!target.HasValue)
            return null;

        // Only scalar fields of these four entities are captured; timestamps are maintained by EF.
        var properties = entry.Properties
            .Where(p => p.Metadata.Name is not (nameof(BaseEntity.CreatedAt) or nameof(BaseEntity.UpdatedAt)))
            .ToArray();
        var changed = properties
            .Where(p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue))
            .ToArray();
        if (entry.State == EntityState.Modified && changed.Length == 0)
            return null;

        var isMember = target.Value.MemberId.HasValue;
        var isSoftDeleted = entry.Entity is ISoftDelete softDelete && softDelete.IsDeleted &&
            entry.State == EntityState.Modified &&
            !entry.Property(nameof(ISoftDelete.IsDeleted)).OriginalValue!.Equals(true);
        var action = entry.State switch
        {
            EntityState.Added => isMember ? ActivityActionType.MemberAdded : ActivityActionType.Created,
            EntityState.Deleted => isMember ? ActivityActionType.MemberRemoved : ActivityActionType.Deleted,
            _ when isSoftDeleted => ActivityActionType.Deleted,
            _ when isMember && changed.Any(p => p.Metadata.Name == nameof(ProjectMember.RoleId))
                => ActivityActionType.RoleChanged,
            _ => ActivityActionType.Updated
        };

        var fields = entry.State == EntityState.Modified && !isSoftDeleted ? changed : properties;
        var before = entry.State == EntityState.Added ? null :
            fields.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue);
        var after = entry.State == EntityState.Deleted ? null :
            fields.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);

        return new ActivityLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            EntityType = target.Value.Type,
            EntityId = target.Value.Id,
            ActionType = action,
            CreatedAt = now,
            UpdatedAt = now,
            DetailsJson = JsonSerializer.Serialize(new
            {
                MemberUserId = target.Value.MemberId,
                Before = before,
                After = after
            }, JsonOptions)
        };
    }
}
