using DATN_BE.Modules.ProjectManagement.Domain.Entities;
using DATN_BE.Modules.ProjectManagement.Domain.Enum;
using DATN_BE.Modules.Task.Application.DTOs;
using DATN_BE.Modules.Task.Domain.Entities;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Task.Application.Commands.CreateBacklogCard
{
    public class CreateBacklogCardCommandHandler : IRequestHandler<CreateBacklogCardCommand, CardDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IProjectAuthorizationService _authorizationService;

        public CreateBacklogCardCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IProjectAuthorizationService authorizationService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _authorizationService = authorizationService;
        }

        public async Task<CardDto> Handle(CreateBacklogCardCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new ForbiddenException("Bạn cần đăng nhập để tạo thẻ công việc.");

            await _authorizationService.EnsureCanModifyProjectAsync(request.ProjectId, cancellationToken);

            var reporter = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken)
                ?? throw new NotFoundException("Người dùng không tồn tại.");

            // 1. Tìm Bảng Scrum trong Dự án để gán Cột cho thẻ
            var scrumBoardQuery = _context.Boards
                .Include(b => b.Columns)
                .Where(b => b.ProjectId == request.ProjectId && b.Type == BoardType.Scrum);

            Board? scrumBoard = null;
            if (request.BoardId.HasValue)
            {
                scrumBoard = await scrumBoardQuery.FirstOrDefaultAsync(b => b.Id == request.BoardId.Value, cancellationToken);
            }

            if (scrumBoard == null)
            {
                scrumBoard = await scrumBoardQuery.OrderBy(b => b.CreatedAt).FirstOrDefaultAsync(cancellationToken);
            }

            if (scrumBoard == null || !scrumBoard.Columns.Any())
            {
                throw new BadRequestException("Dự án hiện chưa có Bảng Scrum nào hoặc Bảng Scrum chưa có Cột để chứa công việc.");
            }

            var targetColumn = scrumBoard.Columns
                .Where(col => !col.IsDoneColumn)
                .OrderBy(col => col.Position)
                .FirstOrDefault() ?? scrumBoard.Columns.OrderBy(col => col.Position).First();

            // 2. Nếu chỉ định SprintId, kiểm tra tính hợp lệ của Sprint
            if (request.SprintId.HasValue)
            {
                var sprint = await _context.Sprints
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == request.SprintId.Value && s.BoardId == scrumBoard.Id, cancellationToken)
                    ?? throw new NotFoundException("Sprint không tồn tại trên Bảng Scrum này.");

                if (sprint.Status == SprintStatus.Closed)
                {
                    throw new BadRequestException("Không thể tạo công việc vào một Sprint đã hoàn thành.");
                }
            }

            // 3. Tính Position tiếp theo trong Cột
            var maxPosition = await _context.Cards
                .Where(c => c.ColumnId == targetColumn.Id)
                .MaxAsync(c => (int?)c.Position, cancellationToken) ?? -1;

            // 4. Tạo thẻ mới cho Scrum
            var newCard = new Card
            {
                ColumnId = targetColumn.Id,
                Title = request.Title.Trim(),
                Description = request.Description,
                Position = maxPosition + 1,
                Type = request.Type,
                Priority = request.Priority,
                StoryPoints = request.StoryPoints,
                SprintId = request.SprintId,
                ReporterId = currentUserId,
                ProgressPercentage = 0
            };

            _context.Cards.Add(newCard);
            await _context.SaveChangesAsync(cancellationToken);

            return new CardDto
            {
                Id = newCard.Id,
                Title = newCard.Title,
                Description = newCard.Description,
                Position = newCard.Position,
                Type = newCard.Type,
                Priority = newCard.Priority,
                StoryPoints = newCard.StoryPoints,
                StartDate = newCard.StartDate,
                DueDate = newCard.DueDate,
                ProgressPercentage = newCard.ProgressPercentage,
                ColumnId = newCard.ColumnId,
                ReporterId = newCard.ReporterId,
                ReporterName = reporter.Username,
                ReporterAvatarUrl = reporter.AvatarUrl,
                SprintId = newCard.SprintId,
                CreatedAt = newCard.CreatedAt,
                CommentsCount = 0
            };
        }
    }
}
