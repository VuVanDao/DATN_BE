using DATN_BE.Modules.Task.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.Task.Application.Queries.GetCardDependencies
{
    public class GetCardDependenciesQuery : IRequest<CardDependenciesResultDto>
    {
        public Guid CardId { get; set; }

        public GetCardDependenciesQuery(Guid cardId)
        {
            CardId = cardId;
        }
    }
}
