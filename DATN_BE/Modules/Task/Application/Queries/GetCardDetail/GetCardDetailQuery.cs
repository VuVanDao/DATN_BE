using DATN_BE.Modules.Task.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.Task.Application.Queries.GetCardDetail
{
    public class GetCardDetailQuery : IRequest<CardDetailDto>
    {
        public Guid CardId { get; set; }

        public GetCardDetailQuery(Guid cardId)
        {
            CardId = cardId;
        }
    }
}
