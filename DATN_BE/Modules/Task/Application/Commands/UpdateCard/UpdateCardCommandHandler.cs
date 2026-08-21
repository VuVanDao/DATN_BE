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
        public UpdateCardCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<CardDto> Handle(UpdateCardCommand request, CancellationToken cancellationToken)
        {
            // tim card theo id
            var targetCard = await _context.Cards.Include(c => c.Assignee).Include(c => c.Reporter)
                .FirstOrDefaultAsync(c => c.Id == request.CardId, cancellationToken) ?? throw new NotFoundException($"Không tìm thấy card với id {request.CardId}");
            
            targetCard.Title = request.Title.Trim();
            targetCard.Description = request.Description;
            targetCard.CoverUrl = request.CoverUrl;
            targetCard.Type = request.Type;
            targetCard.Priority = request.Priority;
            targetCard.AssigneeId = request.AssigneeId;
            targetCard.StartDate = request.StartDate;
            targetCard.DueDate = request.DueDate;
            targetCard.StoryPoints = request.StoryPoints;
            targetCard.ProgressPercentage = request.ProgressPercentage;

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
