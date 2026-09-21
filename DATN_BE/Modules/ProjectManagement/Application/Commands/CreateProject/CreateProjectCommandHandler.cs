using DATN_BE.Modules.ProjectManagement.Application.DTOs;
using DATN_BE.Modules.ProjectManagement.Domain.Entities;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.ProjectManagement.Application.Commands.CreateProject
{
    public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, ProjectDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        public CreateProjectCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }
        public async Task<ProjectDto> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.UserId ?? throw new ForbiddenException("Bạn cần phải đăng nhập vào trước");
            // 1. Kiểm tra Workspace có tồn tại và User có phải thành viên không
            var targetWorkspace = await _context.Workspaces
                .Include(w => w.Members)
                .Include(w => w.Projects)
                .FirstOrDefaultAsync(w => w.Id == request.WorkspaceId) ?? throw new NotFoundException("Workspace không tồn tại");
            // 1.1. chi workspaceOwner moi duoc phep tao project
            if(targetWorkspace.OwnerId != currentUserId)
            {
                throw new ForbiddenException("Chỉ workspaceOwner mới được tạo project");
            }

            var checkUserIncludedInWorkspace = targetWorkspace.Members.Any(wm => wm.UserId == currentUserId) || targetWorkspace.OwnerId == currentUserId;
            if (!checkUserIncludedInWorkspace)
            {
                throw new BadRequestException("Bạn không phải thành viên của workspace này");
            }
            // 2. Chuẩn hóa ProjectKey thành chữ IN HOA (vd: "datn" -> "DATN")
            var normalizedKey = request.ProjectKey.Trim().ToUpperInvariant();
            // 3. Kiểm tra xem ProjectKey đã tồn tại trong Workspace này chưa
            var existProjectKey = targetWorkspace.Projects.Any(p => p.ProjectKey == normalizedKey);
            if (existProjectKey)
            {
                throw new BadRequestException($"Mã dự án {request.ProjectKey} này đã tồn tại trong workspace này");
            }
            var userName = _currentUserService.Username
                ?? throw new NotFoundException("Người dùng không tồn tại.");
            // 4. Tạo Project
            var newProject = new Project
            {
                Name = request.Name,
                ProjectKey = normalizedKey,
                Description = request.Description,
                WorkspaceId = request.WorkspaceId,
                CreatedById = currentUserId,
            };
            _context.Projects.Add(newProject);
            // 5. Tự động tạo 1 Board Kanban mặc định cho Project
            var defaultBoard = new Board
            {
                Title = "New Kanban board",
                Slug = "new-kanban-board",
                Type = Domain.Enum.BoardType.Kanban,
                ProjectId = newProject.Id
            };
            // 6. Tự động tạo sẵn 3 Cột cơ bản cho Board mới
            defaultBoard.Columns.Add(new Column
            {
                Title = "Todo",
                Position = 0,
                IsDoneColumn = false,
                BoardId = defaultBoard.Id,
            });
            defaultBoard.Columns.Add(new Column
            {
                Title = "In Process",
                Position = 1,
                IsDoneColumn = false,
                BoardId = defaultBoard.Id,
            });
            defaultBoard.Columns.Add(new Column
            {
                Title = "Complete",
                Position = 2,
                IsDoneColumn = false,
                BoardId = defaultBoard.Id,
            });

            _context.Boards.Add(defaultBoard);
           
            await _context.SaveChangesAsync();

            return new ProjectDto
            {
                Id = newProject.Id,
                Name = newProject.Name,
                ProjectKey = newProject.ProjectKey,
                Description = newProject.Description,
                WorkspaceId = newProject.WorkspaceId,
                CreatedById = newProject.CreatedById,
                CreatedByName = userName,
                BoardsCount = 1,
                CreatedAt = newProject.CreatedAt
            };
        }
    }
}
