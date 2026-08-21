using DATN_BE.Modules.Task.Application.DTOs;
using DATN_BE.Modules.Task.Domain.Enum;
using MediatR;

namespace DATN_BE.Modules.Task.Application.Commands.UpdateCard
{
    public class UpdateCardCommand :IRequest<CardDto>
    {
        public Guid CardId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CoverUrl { get; set; }
        public CardType Type { get; set; }
        public CardPriority Priority { get; set; }
        public Guid? AssigneeId { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? DueDate { get; set; }
        public int? StoryPoints { get; set; }
        public int ProgressPercentage { get; set; }
    }
}
