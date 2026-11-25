namespace DATN_BE.Modules.Security.Application.DTOs;

public class ActivityLogDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? Username { get; set; }
    public string? UserAvatarUrl { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string? DetailsJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
