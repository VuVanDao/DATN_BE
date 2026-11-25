using DATN_BE.Modules.Identity.Domain.Enum;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Identity.Application.Commands.AssignUserRoles
{
    public class AssignUserRoleCommandHandler : IRequestHandler<AssignUserRoleCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        public AssignUserRoleCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<bool> Handle(AssignUserRoleCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken) 
                ?? throw new NotFoundException("Không tìm thấy người dùng với ID tương ứng.");

            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken) 
                ?? throw new NotFoundException("Không tìm thấy vai trò tương ứng.");

            // Chỉ cho phép gán vai trò có Scope Global ở cấp độ tài khoản hệ thống
            if (role.Scope != RoleScope.Global)
            {
                throw new BadRequestException("Chỉ có thể gán các vai trò toàn cục (Global) cho tài khoản người dùng.");
            }

            user.RoleId = role.Id;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
