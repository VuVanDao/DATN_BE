using DATN_BE.Modules.Task.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.Task.Application.Queries.SearchCardCandidates
{
    public class SearchCardCandidatesQuery : IRequest<List<CardCandidateDto>>
    {
        public Guid CardId { get; set; } // Thẻ hiện tại (để loại trừ chính nó và các thẻ đã liên kết)
        public string? Query { get; set; }

        public SearchCardCandidatesQuery(Guid cardId, string? query)
        {
            CardId = cardId;
            Query = query;
        }
    }
}
