using DATN_BE.Modules.Task.Application.DTOs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Task.Application.Queries.GetCardById
{
    public class GetCardByIdQueryHandler : IRequestHandler<GetCardByIdQuery, CardDetailDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IProjectAuthorizationService _authorizationService;
        public GetCardByIdQueryHandler(IApplicationDbContext context, IProjectAuthorizationService authorizationService)
        {
            _context = context;
            _authorizationService = authorizationService;
        }
        public async Task<CardDetailDto> Handle(GetCardByIdQuery request, CancellationToken cancellationToken)
        {
            var targetCard = await _context.Cards.AsNoTracking()
            .Include(c => c.Column).ThenInclude(c => c.Board)
            .Include(c => c.Assignee)
            .Include(c => c.Reporter)
            .Include(c => c.Comments)
            .FirstOrDefaultAsync(c => c.Id == request.CardId, cancellationToken)
            ?? throw new NotFoundException("Card không tìm thấy");
            await _authorizationService.EnsureCanViewProjectAsync(targetCard.Column.Board.ProjectId, cancellationToken);
            return new CardDetailDto
            {
                Id = targetCard.Id,
                Title = targetCard.Title,
                Description = targetCard.Description,
                Position = targetCard.Position,
                CoverUrl = targetCard.CoverUrl,
                Type = targetCard.Type,
                Priority = targetCard.Priority,
                StoryPoints = targetCard.StoryPoints,
                StartDate = targetCard.StartDate,
                DueDate = targetCard.DueDate,
                ProgressPercentage = targetCard.ProgressPercentage,
                ColumnId = targetCard.ColumnId,
                AssigneeId = targetCard.AssigneeId,
                AssigneeName = targetCard.Assignee?.Username,
                AssigneeAvatarUrl = targetCard.Assignee?.AvatarUrl,
                ReporterId = targetCard.ReporterId,
                ReporterName = targetCard.Reporter?.Username ?? string.Empty,
                SprintId = targetCard   .SprintId,
                CreatedAt = targetCard.CreatedAt,
                Comments = targetCard.Comments
                    .OrderByDescending(cm => cm.CreatedAt)
                    .Select(cm => new CardCommentDto
                    {
                        Id = cm.Id,
                        CardId = cm.CardId,
                        UserId = cm.UserId,
                        Username = cm.User?.Username ?? string.Empty,
                        UserAvatarUrl = cm.User?.AvatarUrl,
                        ContentText = cm.ContentText,
                        CreatedAt = cm.CreatedAt
                    }).ToList()
            };
        }
    }
}
