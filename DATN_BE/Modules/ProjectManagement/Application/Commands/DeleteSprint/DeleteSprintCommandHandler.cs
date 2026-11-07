using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.DeleteSprint
{
    public class DeleteSprintCommandHandler : IRequestHandler<DeleteSprintCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IProjectAuthorizationService _authorizationService;

        public DeleteSprintCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUserService,
            IProjectAuthorizationService authorizationService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _authorizationService = authorizationService;
        }

        public async Task<bool> Handle(DeleteSprintCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new ForbiddenException("Bạn cần đăng nhập để xóa Sprint.");

            var sprint = await _context.Sprints
                .Include(s => s.Cards)
                .FirstOrDefaultAsync(s => s.Id == request.SprintId, cancellationToken)
                ?? throw new NotFoundException("Sprint", request.SprintId);

            await _authorizationService.EnsureCanModifyProjectAsync(sprint.ProjectId, cancellationToken);

            // Các thẻ trong Sprint được trả về Backlog (SprintId = null)
            foreach (var card in sprint.Cards)
            {
                card.SprintId = null;
            }

            _context.Sprints.Remove(sprint);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
