using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.DeleteProject
{
    public class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly IProjectAuthorizationService _authorizationService;

        public DeleteProjectCommandHandler(
            IApplicationDbContext context,
            IProjectAuthorizationService authorizationService)
        {
            _context = context;
            _authorizationService = authorizationService;
        }

        public async Task<bool> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(project => project.Id == request.ProjectId, cancellationToken)
                ?? throw new NotFoundException("Project", request.ProjectId);

            await _authorizationService.EnsureCanManageProjectAsync(project.Id, cancellationToken);

            // Include deleted parents so active descendants are not missed.
            var projectBoards = _context.Boards
                .IgnoreQueryFilters()
                .Where(board => board.ProjectId == project.Id);
            var boardIds = projectBoards.Select(board => board.Id);
            
            var projectColumns = _context.Columns
                .IgnoreQueryFilters()
                .Where(column => boardIds.Contains(column.BoardId));
            var columnIds = projectColumns.Select(column => column.Id);

            var boards = await projectBoards.ToListAsync(cancellationToken);
            var columns = await projectColumns.ToListAsync(cancellationToken);
            var cards = await _context.Cards
                .IgnoreQueryFilters()
                .Where(card => columnIds.Contains(card.ColumnId))
                .ToListAsync(cancellationToken);

            var deletedAt = DateTimeOffset.UtcNow;
            MarkAsDeleted(project, deletedAt);
            foreach (var board in boards)
            {
                MarkAsDeleted(board, deletedAt);
            }
            foreach (var column in columns)
            {
                MarkAsDeleted(column, deletedAt);
            }
            foreach (var card in cards)
            {
                MarkAsDeleted(card, deletedAt);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        private static void MarkAsDeleted(ISoftDelete entity, DateTimeOffset deletedAt)
        {
            if (entity.IsDeleted)
            {
                return;
            }

            entity.IsDeleted = true;
            entity.DeletedAt = deletedAt;
        }
    }
}
