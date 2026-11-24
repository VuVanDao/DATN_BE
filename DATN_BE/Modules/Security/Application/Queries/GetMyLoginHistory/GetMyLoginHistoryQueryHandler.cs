using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Modules.Security.Application.DTOs;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Security.Application.Queries.GetMyLoginHistory
{
    public class GetMyLoginHistoryQueryHandler : IRequestHandler<GetMyLoginHistoryQuery, List<LoginHistoryDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        public GetMyLoginHistoryQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<List<LoginHistoryDto>> Handle(GetMyLoginHistoryQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedException("Bạn chưa đăng nhập."); ;
            var limit = request.Limit > 100 ? 100 : (request.Limit < 1 ? 20 : request.Limit);
            var histories = await _context.LoginHistories
                .Where(lh => lh.UserId == userId)
                .OrderByDescending(lh => lh.CreatedAt)
                .Take(limit)
                .Select(lh => new LoginHistoryDto
                {
                    Id = lh.Id,
                    UserId = lh.UserId,
                    EmailAttempted = lh.EmailAttempted,
                    IpAddress = lh.IpAddress,
                    UserAgent = lh.UserAgent,
                    IsSuccess = lh.IsSuccess,
                    FailureReason = lh.FailureReason,
                    CreatedAt = lh.CreatedAt
                })
                .ToListAsync(cancellationToken);
            return histories;
        }
    }
}
