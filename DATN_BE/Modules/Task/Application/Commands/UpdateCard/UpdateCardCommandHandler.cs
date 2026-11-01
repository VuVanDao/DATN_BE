using DATN_BE.Modules.Identity.Domain.Entities;
using DATN_BE.Modules.Task.Application.DTOs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Task.Application.Commands.UpdateCard
{
    public class UpdateCardCommandHandler : IRequestHandler<UpdateCardCommand, CardDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IProjectAuthorizationService _authorizationService;
        public UpdateCardCommandHandler(IApplicationDbContext context, IProjectAuthorizationService authorizationService)
        {
            _context = context;
            _authorizationService = authorizationService;
        }
        public async Task<CardDto> Handle(UpdateCardCommand request, CancellationToken cancellationToken)
        {
            var targetCard = await _context.Cards.Include(c => c.Assignee).Include(c => c.Reporter)
                .Include(c => c.Column)
                .ThenInclude(c => c.Board)
                .FirstOrDefaultAsync(c => c.Id == request.CardId, cancellationToken)
                ?? throw new NotFoundException($"Không tìm thấy card với id {request.CardId}");
            await _authorizationService.EnsureCanModifyProjectAsync(targetCard.Column.Board.ProjectId, cancellationToken);
            
            targetCard.Title = request.Title.Trim();
            targetCard.Description = request.Description;
            targetCard.CoverUrl = request.CoverUrl;
            targetCard.Type = request.Type;
            targetCard.Priority = request.Priority;
            targetCard.StartDate = request.StartDate;
            targetCard.DueDate = request.DueDate;
            targetCard.StoryPoints = request.StoryPoints;
            targetCard.ProgressPercentage = request.ProgressPercentage;

            if (request.AssigneeId.HasValue)
            {
                if (targetCard.AssigneeId != request.AssigneeId.Value || targetCard.Assignee == null)
                {
                    var assignee = await _context.Users
                        .FirstOrDefaultAsync(u => u.Id == request.AssigneeId.Value &&
                            u.ProjectMembers.Any(pm => pm.ProjectId == targetCard.Column.Board.ProjectId), cancellationToken)
                        ?? throw new BadRequestException("Người được gán thẻ không tồn tại hoặc không thuộc Project này.");

                    targetCard.AssigneeId = request.AssigneeId;
                    targetCard.Assignee = assignee;
                }
            }
            else
            {
                targetCard.AssigneeId = null;
                targetCard.Assignee = null;
            }

            await _context.SaveChangesAsync(cancellationToken);
            
            return new CardDto
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
                CreatedAt = targetCard.CreatedAt
            };
        }
    }
}
