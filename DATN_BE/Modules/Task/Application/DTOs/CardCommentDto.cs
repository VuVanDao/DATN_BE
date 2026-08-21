namespace DATN_BE.Modules.Task.Application.DTOs
{
    public class CardCommentDto
    {
        public Guid Id { get; set; }
        public Guid CardId { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? UserAvatarUrl { get; set; }
        public string ContentText { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
    }
}
