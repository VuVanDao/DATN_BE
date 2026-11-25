using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Modules.Security.Domain.Enum;
using DATN_BE.Shared.Application.Events;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Modules.ProjectManagement.Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using DATN_BE.Modules.ProjectManagement.Presentation.Hubs;
using Microsoft.Extensions.Logging;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.CreateColumn
{
    public class CreateColumnCommandHandler : IRequestHandler<CreateColumnCommand, ColumnDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IProjectAuthorizationService _authorizationService;
        private readonly IHubContext<BoardHub>? _hubContext;
        private readonly ICurrentUserService? _currentUserService;
        private readonly ILogger<CreateColumnCommandHandler>? _logger;
        private readonly IMediator? _mediator;

        public CreateColumnCommandHandler(
            IApplicationDbContext context,
            IProjectAuthorizationService authorizationService,
            IHubContext<BoardHub>? hubContext = null,
            ICurrentUserService? currentUserService = null,
            ILogger<CreateColumnCommandHandler>? logger = null,
            IMediator? mediator = null)
        {
            _context = context;
            _authorizationService = authorizationService;
            _hubContext = hubContext;
            _currentUserService = currentUserService;
            _logger = logger;
            _mediator = mediator;
        }

        public async Task<ColumnDto> Handle(CreateColumnCommand request, CancellationToken cancellationToken)
        {
            var targetBoard = await _context.Boards
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == request.BoardId, cancellationToken)
                ?? throw new NotFoundException("Board", request.BoardId);

            await _authorizationService.EnsureCanModifyProjectAsync(targetBoard.ProjectId, cancellationToken);

            // Tự động tính vị trí Position tiếp theo (max + 1)
            var maxPosition = await _context.Columns
                .Where(c => c.BoardId == request.BoardId)
                .Select(c => (int?)c.Position)
                .MaxAsync(cancellationToken) ?? -1;

            var newColumn = new Column
            {
                Title = request.Title,
                BoardId = request.BoardId,
                IsDoneColumn = request.IsDoneColumn,
                Position = maxPosition + 1
            };

            _context.Columns.Add(newColumn);
            await _context.SaveChangesAsync(cancellationToken);

            if (_mediator != null && _currentUserService?.UserId is Guid actorId)
            {
                await _mediator.Publish(new EntityActivityLoggedEvent(
                    actorId,
                    ActivityEntityType.Column,
                    newColumn.Id,
                    ActivityActionType.Created,
                    new
                    {
                        columnTitle = newColumn.Title,
                        boardId = newColumn.BoardId,
                        position = newColumn.Position,
                        isDoneColumn = newColumn.IsDoneColumn
                    }), cancellationToken);
            }

            if (_hubContext != null)
            {
                try
                {
                    await _hubContext.Clients
                        .Group(BoardHub.GroupName(request.BoardId))
                        .SendAsync(
                            BoardHub.ColumnCreated,
                            new ColumnCreatedMessage(
                                newColumn.BoardId,
                                newColumn.Id,
                                newColumn.Title,
                                newColumn.Position,
                                newColumn.IsDoneColumn,
                                _currentUserService?.UserId ?? Guid.Empty,
                                DateTimeOffset.UtcNow),
                            CancellationToken.None);
                }
                catch (Exception exception)
                {
                    _logger?.LogWarning(exception, "Không thể phát sự kiện ColumnCreated cho board {BoardId}.", request.BoardId);
                }
            }

            return new ColumnDto
            {
                Id = newColumn.Id,
                Title = newColumn.Title,
                Position = newColumn.Position,
                IsDoneColumn = newColumn.IsDoneColumn,
                BoardId = newColumn.BoardId
            };
        }
    }
}
