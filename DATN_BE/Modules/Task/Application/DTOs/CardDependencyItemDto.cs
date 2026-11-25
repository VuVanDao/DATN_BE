using DATN_BE.Modules.Task.Domain.Enum;

namespace DATN_BE.Modules.Task.Application.DTOs
{
    public class CardDependencyItemDto
    {
        public Guid DependencyId { get; set; }
        public Guid CardId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? ColumnTitle { get; set; }
        public bool IsDone { get; set; }
        public CardType Type { get; set; }
        public CardPriority Priority { get; set; }
        public string? AssigneeName { get; set; }
        public string? AssigneeAvatarUrl { get; set; }
        public string? Direction { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class CardDependenciesResultDto
    {
        public List<CardDependencyItemDto> BlockedBy { get; set; } = new();
        public List<CardDependencyItemDto> Blocking { get; set; } = new();
    }

    public class CardCandidateDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? ColumnTitle { get; set; }
        public bool IsDone { get; set; }
        public CardType Type { get; set; }
        public CardPriority Priority { get; set; }
        public string? AssigneeName { get; set; }
    }
}
