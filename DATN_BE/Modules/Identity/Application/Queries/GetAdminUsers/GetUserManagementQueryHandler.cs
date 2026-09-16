using DATN_BE.Modules.Identity.Application.DTOs;
using DATN_BE.Modules.Identity.Domain.Entities;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Application.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Identity.Application.Queries.GetAdminUsers
{
    public class GetUserManagementQueryHandler : IRequestHandler<GetUserManagementQuery, PagedResult<UserManagementDto>>
    {
        private readonly IApplicationDbContext _context;
        public GetUserManagementQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<PagedResult<UserManagementDto>> Handle(GetUserManagementQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .AsNoTracking();
            // 1. Tìm kiếm theo Email hoặc Username
            if (!string.IsNullOrEmpty(request.Search))
            {
                var search = request.Search.Trim().ToLower();
                query = query.Where(q => q.Email.ToLower().Contains(search) || q.Username.ToLower().Contains(search));
            }
            // 2. Lọc theo trạng thái Active
            if (request.IsActive.HasValue)
            {
                query = query.Where(q => q.IsActive == request.IsActive);
            }
            // 3. Đếm tổng số bản ghi
            var totalCount = await query.CountAsync(cancellationToken);
            // 4. Phân trang
            var pageIndex = request.PageIndex < 1 ? 1 : request.PageIndex;
            var pageSize = request.PageSize < 1 ? 10 : (request.PageSize > 100 ? 100 : request.PageSize);
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserManagementDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    Username = u.Username,
                    AvatarUrl = u.AvatarUrl,
                    Bio = u.Bio,
                    IsActive = u.IsActive,
                    IsDeleted = u.IsDeleted,
                    CreatedAt = u.CreatedAt,
                    Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
                }).ToListAsync(cancellationToken);
            return new PagedResult<UserManagementDto>(users, totalCount, pageIndex, pageSize);
        }
    }
}
