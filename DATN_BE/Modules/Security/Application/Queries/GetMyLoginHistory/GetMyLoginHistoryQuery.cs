using DATN_BE.Modules.Security.Application.DTOs;
using MediatR;

namespace DATN_BE.Modules.Security.Application.Queries.GetMyLoginHistory
{
    public class GetMyLoginHistoryQuery : IRequest<List<LoginHistoryDto>>
    {
        public int Limit { get; set; } = 20; // Mặc định lấy 20 lần gần nhất
    }
}
