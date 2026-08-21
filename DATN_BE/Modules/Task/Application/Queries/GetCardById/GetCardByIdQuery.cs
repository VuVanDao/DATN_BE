using DATN_BE.Modules.Task.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.Task.Application.Queries.GetCardById
{
    public class GetCardByIdQuery : IRequest<CardDetailDto>
    {
        public Guid CardId { get; set; }
        public GetCardByIdQuery(Guid cardId)
        {
            CardId = cardId;
        }
    }
}
