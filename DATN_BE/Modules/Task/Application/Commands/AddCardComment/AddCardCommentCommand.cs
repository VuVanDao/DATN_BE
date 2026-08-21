using DATN_BE.Modules.Task.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.Task.Application.Commands.AddCardComment
{
    public class AddCardCommentCommand : IRequest<CardCommentDto>
    {
        public Guid CardId { get; set; }
        public string ContentText { get; set; } = string.Empty;
    }
}
