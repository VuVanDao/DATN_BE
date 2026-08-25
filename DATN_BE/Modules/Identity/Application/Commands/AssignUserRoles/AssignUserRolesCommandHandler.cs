using DATN_BE.Modules.Identity.Domain.Entities;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Identity.Application.Commands.AssignUserRoles
{
    public class AssignUserRolesCommandHandler : IRequestHandler<AssignUserRolesCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        public AssignUserRolesCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<bool> Handle(AssignUserRolesCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken) ?? throw new NotFoundException("Không tìm thấy user với id tương ứng");
            // Xóa các vai trò cũ
            _context.UserRoles.RemoveRange(user.UserRoles);

            // Gán danh sách vai trò mới
            var validRoleIds = await _context.Roles
                .Where(r => request.RoleIds.Contains(r.Id))
                .Select(r => r.Id)
                .ToListAsync(cancellationToken);

            foreach (var roleId in validRoleIds)
            {
                _context.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = roleId,
                    AssignedAt = DateTimeOffset.UtcNow
                });
            }
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
