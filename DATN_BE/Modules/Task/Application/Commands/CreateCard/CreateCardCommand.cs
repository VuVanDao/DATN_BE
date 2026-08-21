using DATN_BE.Modules.Task.Application.DTOs;
using DATN_BE.Modules.Task.Domain.Enum;
using MediatR;

namespace DATN_BE.Modules.Task.Application.Commands.CreateCard
{
    public class CreateCardCommand : IRequest<CardDto>
    {
        public Guid ColumnId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public CardType Type { get; set; } = CardType.Task;
        public CardPriority Priority { get; set; } = CardPriority.Medium;
        public Guid? AssigneeId { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? DueDate { get; set; }
        public int? StoryPoints { get; set; }
    }
}
