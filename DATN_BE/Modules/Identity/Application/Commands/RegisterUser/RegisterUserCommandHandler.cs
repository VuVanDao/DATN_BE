using DATN_BE.Modules.Identity.Application.DTOs;
using DATN_BE.Modules.Identity.Application.Interfaces;
using DATN_BE.Modules.Identity.Domain.Entities;
using DATN_BE.Modules.Identity.infrastructure.Services;
using DATN_BE.Shared.Application.Exceptions;
using DATN_BE.Shared.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE.Modules.Identity.Application.Commands.RegisterUser
{
    public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, AuthResponseDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        public RegisterUserCommandHandler(IApplicationDbContext context, IJwtTokenGenerator jwtTokenGenerator)
        {
            _context = context;
            _jwtTokenGenerator = jwtTokenGenerator;
        }
        public async Task<AuthResponseDto> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra Email đã tồn tại chưa
            var emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower() && !u.IsDeleted, cancellationToken);
            if(emailExists)
            {
                throw new BadRequestException("Địa chỉ email này đã tồn tại");
            }
            // 2. Kiểm tra Username đã tồn tại chưa
            var usernameExists = await _context.Users.AnyAsync(u => u.Username.ToLower() == request.Username.ToLower() && !u.IsDeleted, cancellationToken);
            if(usernameExists)
            {
                throw new BadRequestException($"Username {request.Username} này đã tồn tại");
            }
            // 3. Băm mật khẩu bằng BCrypt
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            // 4. Khởi tạo Entity User
            var user = new User
            {
                Email = request.Email.Trim().ToLower(),
                Username = request.Username.Trim(),
                PasswordHash = passwordHash,
                Bio = request.Bio,
                IsActive = true
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);
            // 5. Sinh JWT Token
            var token = _jwtTokenGenerator.GenerateToken(user, new List<string>());

            return new AuthResponseDto
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                AvatarUrl = user.AvatarUrl,
                Token = token,
                Roles = new List<string>()
            };
        }
    }
}
