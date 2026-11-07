using DATN_BE.Modules.Identity.Domain.Entities;
using DATN_BE.Modules.Task.Application.DTOs;
using DATN_BE.Modules.Task.Domain.Entities;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Task.Application.Commands.CreateCard
{
    public class CreateCardCommandHandler : IRequestHandler<CreateCardCommand, CardDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IProjectAuthorizationService _authorizationService;
        public CreateCardCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IProjectAuthorizationService authorizationService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _authorizationService = authorizationService;
        }
        public async Task<CardDto> Handle(CreateCardCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new ForbiddenException("Bạn cần đăng nhập để tạo Thẻ công việc.");
            // Kiểm tra xem cột có tồn tại không
            var targetColumn = await _context.Columns.Include(c => c.Board)
                .FirstOrDefaultAsync(c => c.Id == request.ColumnId, cancellationToken)
                ?? throw new NotFoundException("Cột không tồn tại.");
            if (targetColumn.Board == null)
            {
                throw new NotFoundException("Bảng của cột này không tồn tại hoặc đã bị xoá.");
            }
            await _authorizationService.EnsureCanModifyProjectAsync(targetColumn.Board.ProjectId, cancellationToken);
            // Kiểm tra xem người dùng hiện tại có tồn tại không
            var reporter = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken)
                ?? throw new NotFoundException("Người dùng không tồn tại.");

            User? assignee = null;
            if (request.AssigneeId.HasValue)
            {
                assignee = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == request.AssigneeId.Value &&
                        u.ProjectMembers.Any(pm => pm.ProjectId == targetColumn.Board.ProjectId), cancellationToken)
                    ?? throw new BadRequestException("Người được gán thẻ không tồn tại hoặc không thuộc Project này.");
            }

            // 1. Tự động tính Position của card tiếp theo trong Cột đó
            var maxPosition = await _context.Cards.Where(c => c.ColumnId == request.ColumnId)
                .MaxAsync(c => (int?)c.Position, cancellationToken) ?? -1;
            // 2. Tạo Thẻ mới
            var newCard = new Card
            {
                ColumnId = request.ColumnId,
                Title = request.Title.Trim(),
                Description = request.Description,
                Position = maxPosition + 1,
                Type = request.Type,
                Priority = request.Priority,
                AssigneeId = request.AssigneeId,
                ReporterId = currentUserId,
                StartDate = request.StartDate,
                DueDate = request.DueDate,
                StoryPoints = request.StoryPoints,
                SprintId = request.SprintId,
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
                AssigneeId = newCard.AssigneeId,
                AssigneeName = assignee?.Username,
                AssigneeAvatarUrl = assignee?.AvatarUrl,
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
