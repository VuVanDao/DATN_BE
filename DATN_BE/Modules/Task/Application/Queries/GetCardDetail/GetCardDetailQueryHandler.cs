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
                    IsCompleted = c.IsCompleted,
                    ColumnId = c.ColumnId,
                    AssigneeId = c.AssigneeId,

                    AssigneeName = c.Assignee == null ? null : c.Assignee.Username,
                    AssigneeAvatarUrl = c.Assignee == null ? null : c.Assignee.AvatarUrl,

                    ReporterId = c.ReporterId,
                    ReporterName = c.Reporter.Username,
                    ReporterAvatarUrl = c.Reporter.AvatarUrl,

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

            var blockedBy = await _context.CardDependencies
                .AsNoTracking()
                .Where(cd => cd.BlockedCardId == targetCard.Id)
                .OrderByDescending(cd => cd.CreatedAt)
                .Select(cd => new CardDependencyItemDto
                {
                    DependencyId = cd.Id,
                    CardId = cd.BlockingCard.Id,
                    Title = cd.BlockingCard.Title,
                    ColumnTitle = cd.BlockingCard.Column != null ? cd.BlockingCard.Column.Title : null,
                    IsDone = (cd.BlockingCard.Column != null && cd.BlockingCard.Column.IsDoneColumn) || cd.BlockingCard.IsCompleted,
                    Type = cd.BlockingCard.Type,
                    Priority = cd.BlockingCard.Priority,
                    AssigneeName = cd.BlockingCard.Assignee != null ? cd.BlockingCard.Assignee.Username : null,
                    AssigneeAvatarUrl = cd.BlockingCard.Assignee != null ? cd.BlockingCard.Assignee.AvatarUrl : null,
                    Direction = "BlockedBy",
                    CreatedAt = cd.CreatedAt
                }).ToListAsync(cancellationToken);

            var blocking = await _context.CardDependencies
                .AsNoTracking()
                .Where(cd => cd.BlockingCardId == targetCard.Id)
                .OrderByDescending(cd => cd.CreatedAt)
                .Select(cd => new CardDependencyItemDto
                {
                    DependencyId = cd.Id,
                    CardId = cd.BlockedCard.Id,
                    Title = cd.BlockedCard.Title,
                    ColumnTitle = cd.BlockedCard.Column != null ? cd.BlockedCard.Column.Title : null,
                    IsDone = (cd.BlockedCard.Column != null && cd.BlockedCard.Column.IsDoneColumn) || cd.BlockedCard.IsCompleted,
                    Type = cd.BlockedCard.Type,
                    Priority = cd.BlockedCard.Priority,
                    AssigneeName = cd.BlockedCard.Assignee != null ? cd.BlockedCard.Assignee.Username : null,
                    AssigneeAvatarUrl = cd.BlockedCard.Assignee != null ? cd.BlockedCard.Assignee.AvatarUrl : null,
                    Direction = "Blocking",
                    CreatedAt = cd.CreatedAt
                }).ToListAsync(cancellationToken);

            targetCard.BlockedBy = blockedBy;
            targetCard.Blocking = blocking;
            targetCard.BlockedByCount = blockedBy.Count(d => !d.IsDone);
            targetCard.IsBlocked = targetCard.BlockedByCount > 0;

            return targetCard;
        }
    }
}
