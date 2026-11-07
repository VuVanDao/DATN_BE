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
        private readonly ICloudinaryService? _cloudinaryService;

        public UpdateCardCommandHandler(
            IApplicationDbContext context,
            IProjectAuthorizationService authorizationService,
            ICloudinaryService? cloudinaryService = null)
        {
            _context = context;
            _authorizationService = authorizationService;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<CardDto> Handle(UpdateCardCommand request, CancellationToken cancellationToken)
        {
            var targetCard = await _context.Cards.Include(c => c.Assignee).Include(c => c.Reporter)
                .Include(c => c.Column)
                .ThenInclude(c => c.Board)
                .Include(c => c.Comments)
                .FirstOrDefaultAsync(c => c.Id == request.CardId, cancellationToken)
                ?? throw new NotFoundException($"Không tìm thấy card với id {request.CardId}");
            await _authorizationService.EnsureCanModifyProjectAsync(targetCard.Column.Board.ProjectId, cancellationToken);
            
            targetCard.Title = request.Title.Trim();
            targetCard.Description = request.Description;

            // Xử lý ảnh bìa tương tự Workspace: ưu tiên file upload lên Cloudinary, hoặc nhận URL trực tiếp
            if (request.CoverImage is { Length: > 0 })
            {
                if (_cloudinaryService != null)
                {
                    targetCard.CoverUrl = await _cloudinaryService.UploadCardCoverAsync(request.CoverImage, targetCard.Id.ToString(), cancellationToken);
                }
            }
            else if (!string.IsNullOrWhiteSpace(request.CoverUrl) && request.CoverUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            {
                if (_cloudinaryService != null)
                {
                    targetCard.CoverUrl = await _cloudinaryService.UploadCardCoverFromDataUriAsync(request.CoverUrl, targetCard.Id.ToString(), cancellationToken);
                }
            }
            else
            {
                targetCard.CoverUrl = string.IsNullOrWhiteSpace(request.CoverUrl) ? null : request.CoverUrl;
            }

            targetCard.Type = request.Type;
            targetCard.Priority = request.Priority;
            targetCard.StartDate = request.StartDate;
            targetCard.DueDate = request.DueDate;
            targetCard.StoryPoints = request.StoryPoints;
            targetCard.ProgressPercentage = request.ProgressPercentage;

            if (request.SprintId.HasValue)
            {
                targetCard.SprintId = request.SprintId.Value;
            }

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
                ReporterAvatarUrl = targetCard.Reporter?.AvatarUrl,
                SprintId = targetCard.SprintId,
                CreatedAt = targetCard.CreatedAt,
                CommentsCount = targetCard.Comments.Count
            };
        }
    }
}
