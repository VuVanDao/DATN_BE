using System.Text.Json.Serialization;
using DATN_BE.Modules.Task.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.Task.Application.Commands.AddCardDependency
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DependencyDirection
    {
        BlockedBy = 0, // Thẻ CardId bị chặn bởi TargetCardId (TargetCardId phải làm xong trước)
        Blocking = 1   // Thẻ CardId đang chặn TargetCardId (CardId phải làm xong trước)
    }

    public class AddCardDependencyRequest
    {
        public Guid TargetCardId { get; set; }
        
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DependencyDirection Direction { get; set; } = DependencyDirection.BlockedBy;
    }

    public class AddCardDependencyCommand : IRequest<CardDependencyItemDto>
    {
        public Guid CardId { get; set; }
        public Guid TargetCardId { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DependencyDirection Direction { get; set; } = DependencyDirection.BlockedBy;
    }
}
