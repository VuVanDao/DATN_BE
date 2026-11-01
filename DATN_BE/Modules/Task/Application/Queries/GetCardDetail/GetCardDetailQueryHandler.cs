using DATN_BE.Modules.Task.Application.DTOs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Task.Application.Queries.GetCardDetail
{
    public class GetCardDetailQueryHandler : IRequestHandler<GetCardDetailQuery, CardDetailDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IProjectAuthorizationService _authorizationService;

        public GetCardDetailQueryHandler(IApplicationDbContext context, IProjectAuthorizationService authorizationService)
        {
            _context = context;
            _authorizationService = authorizationService;
        }

        public async Task<CardDetailDto> Handle(GetCardDetailQuery request, CancellationToken cancellationToken)
        {
            var cardInfoToCheckAccessCanView = await _context.Cards
                .AsNoTracking()
                .Where(c => c.Id == request.CardId)
                .Select(c => new
                {
                    c.Column.Board.ProjectId,
                })
                .FirstOrDefaultAsync(cancellationToken) ?? throw new NotFoundException("Card không tồn tại trong hệ thống");

            await _authorizationService.EnsureCanViewProjectAsync(cardInfoToCheckAccessCanView.ProjectId, cancellationToken);

            var targetCard = await _context.Cards
                .AsNoTracking()
                .Where(c => c.Id == request.CardId)
                .Select(c => new CardDetailDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    Position = c.Position,
                    CoverUrl = c.CoverUrl,
                    Type = c.Type,
                    Priority = c.Priority,
                    StoryPoints = c.StoryPoints,
                    StartDate = c.StartDate,
                    DueDate = c.DueDate,
                    ProgressPercentage = c.ProgressPercentage,
                    ColumnId = c.ColumnId,
                    AssigneeId = c.AssigneeId,

                    AssigneeName = c.Assignee == null ? null : c.Assignee.Username,
                    AssigneeAvatarUrl = c.Assignee == null ? null : c.Assignee.AvatarUrl,

                    ReporterId = c.ReporterId,
                    ReporterName = c.Reporter.Username,

                    SprintId = c.SprintId,
                    CreatedAt = c.CreatedAt,
                })
                .FirstOrDefaultAsync(cancellationToken) ?? throw new NotFoundException("Card không tồn tại trong hệ thống");

            var cardComments = await _context.CardComments
                .AsNoTracking()
                .Where(ccmt => ccmt.CardId == targetCard.Id && ccmt.User.IsActive && !ccmt.User.IsDeleted)
                .OrderByDescending(ccmt => ccmt.CreatedAt)
                .Select(ccmt => new CardCommentDto
                {
                    Id = ccmt.Id,
                    UserId = ccmt.UserId,
                    Username = ccmt.User.Username,
                    UserAvatarUrl = ccmt.User.AvatarUrl,
                    CardId = ccmt.CardId,
                    ContentText = ccmt.ContentText,
                    CreatedAt = ccmt.CreatedAt
                }).ToListAsync(cancellationToken);

            targetCard.Comments = cardComments;

            return targetCard;
        }
    }
}
