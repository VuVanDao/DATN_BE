using DATN_BE.Modules.Task.Application.DTOs;
using DATN_BE.Modules.Task.Domain.Enum;
using MediatR;

namespace DATN_BE.Modules.Task.Application.Commands.CreateBacklogCard
{
    public class CreateBacklogCardCommand : IRequest<CardDto>
    {
        public Guid ProjectId { get; set; }
        public Guid? BoardId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public CardType Type { get; set; } = CardType.Task;
        public CardPriority Priority { get; set; } = CardPriority.Medium;
        public int? StoryPoints { get; set; }
        public Guid? SprintId { get; set; }
    }
}
