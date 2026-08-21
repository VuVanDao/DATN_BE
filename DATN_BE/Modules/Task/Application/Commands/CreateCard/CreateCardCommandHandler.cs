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
        public CreateCardCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }
        public async Task<CardDto> Handle(CreateCardCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new ForbiddenException("Bạn cần đăng nhập để tạo Thẻ công việc.");
            // Kiểm tra xem cột có tồn tại không
            var columnExists = await _context.Columns.AnyAsync(c => c.Id == request.ColumnId, cancellationToken);
            if (!columnExists)
            {
                throw new NotFoundException("Cột không tồn tại.");
            }
            // Kiểm tra xem người dùng hiện tại có tồn tại không
            var reporter = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken)
                ?? throw new NotFoundException("Người dùng không tồn tại.");
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
                ProgressPercentage = 0
            };
            _context.Cards.Add(newCard);
            await _context.SaveChangesAsync(cancellationToken);
            // Lấy thông tin người được gán thẻ (Assignee) nếu có
            User? assignee = null;
            if (request.AssigneeId.HasValue)
            {
                assignee = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.AssigneeId.Value, cancellationToken) ?? throw new NotFoundException("Người được gán thẻ (Assignee) không tồn tại.");
            }
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
                CreatedAt = newCard.CreatedAt
            };
        }
    }
}
