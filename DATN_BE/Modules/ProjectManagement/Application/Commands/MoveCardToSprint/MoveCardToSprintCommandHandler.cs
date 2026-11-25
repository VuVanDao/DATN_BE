using DATN_BE.Modules.ProjectManagement.Domain.Enum;
using DATN_BE.Modules.ProjectManagement.Presentation.Hubs;
using DATN_BE.Modules.Task.Application.DTOs;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.MoveCardToSprint
{
    public class MoveCardToSprintCommandHandler : IRequestHandler<MoveCardToSprintCommand, CardDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IProjectAuthorizationService _authorizationService;
        private readonly IHubContext<BoardHub>? _hubContext;
        private readonly ILogger<MoveCardToSprintCommandHandler>? _logger;

        public MoveCardToSprintCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IProjectAuthorizationService authorizationService,
            IHubContext<BoardHub>? hubContext = null,
            ILogger<MoveCardToSprintCommandHandler>? logger = null)
        {
            _context = context;
            _currentUserService = currentUserService;
            _authorizationService = authorizationService;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<CardDto> Handle(MoveCardToSprintCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new UnauthorizedException("Bạn cần đăng nhập để thao tác.");

            var card = await _context.Cards
                .Include(c => c.Column)
                    .ThenInclude(col => col.Board)
                .Include(c => c.Assignee)
                .Include(c => c.Reporter)
                .Include(c => c.Comments)
                .FirstOrDefaultAsync(c => c.Id == request.CardId, cancellationToken)
                ?? throw new NotFoundException("Card", request.CardId);

            var projectId = card.Column?.Board?.ProjectId
                ?? throw new NotFoundException("Không tìm thấy Dự án của thẻ này.");

            // 1. Kiểm tra quyền sửa đổi cơ bản trong dự án (ProjectMember, ProjectLeader, WorkspaceOwner)
            await _authorizationService.EnsureCanModifyProjectAsync(projectId, cancellationToken);

            if (card.Column?.Board?.Type != BoardType.Scrum)
            {
                throw new BadRequestException("Chỉ các thẻ thuộc mô hình Scrum mới có thể đưa vào Sprint hoặc Backlog.");
            }

            var oldSprintId = card.SprintId;
            var targetSprintId = request.SprintId;
            var boardId = card.Column!.BoardId;

            if (oldSprintId == targetSprintId)
            {
                // =========================================================================
                // TH1: SẮP XẾP LẠI THỨ TỰ (REORDER) TRONG CÙNG CONTAINER (CÙNG SPRINT HOẶC CÙNG PRODUCT BACKLOG)
                // =========================================================================
                if (request.NewPosition.HasValue)
                {
                    // Nếu đang sắp xếp thứ tự trong một Sprint
                    if (targetSprintId.HasValue)
                    {
                        var sprint = await _context.Sprints
                            .AsNoTracking()
                            .FirstOrDefaultAsync(s => s.Id == targetSprintId.Value, cancellationToken)
                            ?? throw new NotFoundException("Sprint", targetSprintId.Value);

                        if (sprint.Status == SprintStatus.Closed)
                        {
                            throw new BadRequestException("Không thể sắp xếp công việc trong một Sprint đã hoàn thành.");
                        }

                        // Nếu Sprint đang chạy (Active) -> Bắt buộc là WorkspaceOwner hoặc ProjectLeader mới được thay đổi thứ tự ưu tiên
                        if (sprint.Status == SprintStatus.Active)
                        {
                            await _authorizationService.EnsureCanManageProjectAsync(projectId, cancellationToken);
                        }
                    }

                    var cardsInContainer = await _context.Cards
                        .Where(c => c.Column.BoardId == boardId && c.SprintId == targetSprintId && c.Id != card.Id)
                        .OrderBy(c => c.Position)
                        .ToListAsync(cancellationToken);

                    var newPos = Math.Clamp(request.NewPosition.Value, 0, cardsInContainer.Count);
                    cardsInContainer.Insert(newPos, card);

                    for (int i = 0; i < cardsInContainer.Count; i++)
                    {
                        cardsInContainer[i].Position = i;
                    }
                }
            }
            else
            {
                // =========================================================================
                // TH2: KÉO THẢ SANG CONTAINER KHÁC (GIỮA BACKLOG VÀ SPRINT, HOẶC GIỮA 2 SPRINT)
                // =========================================================================
                bool isSourceActive = false;
                if (oldSprintId.HasValue)
                {
                    var sourceSprint = await _context.Sprints
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Id == oldSprintId.Value, cancellationToken);
                    isSourceActive = sourceSprint?.Status == SprintStatus.Active;
                }

                bool isTargetActive = false;
                if (targetSprintId.HasValue)
                {
                    var targetSprint = await _context.Sprints
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Id == targetSprintId.Value && s.ProjectId == projectId, cancellationToken)
                        ?? throw new NotFoundException("Sprint", targetSprintId.Value);

                    if (targetSprint.BoardId != boardId)
                    {
                        throw new BadRequestException("Không thể đưa thẻ vào Sprint của một Bảng Scrum khác.");
                    }

                    if (targetSprint.Status == SprintStatus.Closed)
                    {
                        throw new BadRequestException("Không thể đưa công việc vào một Sprint đã đóng.");
                    }

                    isTargetActive = targetSprint.Status == SprintStatus.Active;
                }

                // Nếu thêm/bớt thẻ làm thay đổi phạm vi của Sprint đang chạy -> Bắt buộc là WorkspaceOwner hoặc ProjectLeader!
                if (isSourceActive || isTargetActive)
                {
                    await _authorizationService.EnsureCanManageProjectAsync(projectId, cancellationToken);
                }

                // 1. Sắp xếp lại container nguồn (loại bỏ card)
                var cardsInOldContainer = await _context.Cards
                    .Where(c => c.Column.BoardId == boardId && c.SprintId == oldSprintId && c.Id != card.Id)
                    .OrderBy(c => c.Position)
                    .ToListAsync(cancellationToken);

                for (int i = 0; i < cardsInOldContainer.Count; i++)
                {
                    cardsInOldContainer[i].Position = i;
                }

                // 2. Chèn card vào container đích
                var cardsInTargetContainer = await _context.Cards
                    .Where(c => c.Column.BoardId == boardId && c.SprintId == targetSprintId && c.Id != card.Id)
                    .OrderBy(c => c.Position)
                    .ToListAsync(cancellationToken);

                card.SprintId = targetSprintId;

                // Nếu thẻ đang ở cột Done mà bị kéo sang container khác, đưa về cột chưa hoàn thành đầu tiên
                if (card.Column != null && card.Column.IsDoneColumn)
                {
                    var defaultColumn = await _context.Columns
                        .Where(col => col.BoardId == boardId && !col.IsDoneColumn)
                        .OrderBy(col => col.Position)
                        .FirstOrDefaultAsync(cancellationToken)
                        ?? await _context.Columns
                            .Where(col => col.BoardId == boardId)
                            .OrderBy(col => col.Position)
                            .FirstOrDefaultAsync(cancellationToken);

                    if (defaultColumn != null)
                    {
                        card.ColumnId = defaultColumn.Id;
                        card.IsCompleted = defaultColumn.IsDoneColumn;
                    }
                    else
                    {
                        card.IsCompleted = false;
                    }
                }

                var targetPos = request.NewPosition.HasValue
                    ? Math.Clamp(request.NewPosition.Value, 0, cardsInTargetContainer.Count)
                    : cardsInTargetContainer.Count;

                cardsInTargetContainer.Insert(targetPos, card);

                for (int i = 0; i < cardsInTargetContainer.Count; i++)
                {
                    cardsInTargetContainer[i].Position = i;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            var result = new CardDto
            {
                Id = card.Id,
                Title = card.Title,
                Description = card.Description,
                Position = card.Position,
                CoverUrl = card.CoverUrl,
                Type = card.Type,
                Priority = card.Priority,
                StoryPoints = card.StoryPoints,
                StartDate = card.StartDate,
                DueDate = card.DueDate,
                IsCompleted = card.IsCompleted,
                ColumnId = card.ColumnId,
                AssigneeId = card.AssigneeId,
                AssigneeName = card.Assignee?.Username,
                AssigneeAvatarUrl = card.Assignee?.AvatarUrl,
                ReporterId = card.ReporterId,
                ReporterName = card.Reporter?.Username ?? string.Empty,
                ReporterAvatarUrl = card.Reporter?.AvatarUrl,
                SprintId = card.SprintId,
                CreatedAt = card.CreatedAt,
                CommentsCount = card.Comments.Count
            };

            if (_hubContext != null)
            {
                try
                {
                    await _hubContext.Clients
                        .Group(BoardHub.GroupName(boardId))
                        .SendAsync(
                            BoardHub.CardSprintChanged,
                            new CardSprintChangedMessage(
                                boardId,
                                result,
                                oldSprintId,
                                card.SprintId,
                                currentUserId,
                                DateTimeOffset.UtcNow),
                            CancellationToken.None);
                }
                catch (Exception exception)
                {
                    _logger?.LogWarning(exception, "Không thể phát sự kiện CardSprintChanged cho board {BoardId}.", boardId);
                }
            }

            return result;
        }
    }
}
