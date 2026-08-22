using DATN_BE.Modules.Identity.Application.Interfaces;
using DATN_BE.Modules.Identity.Domain.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace DATN_BE.Modules.Identity.infrastructure.Services
{
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly IConfiguration _configuration;
        public JwtTokenGenerator(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(User user, IEnumerable<string> roles)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credential = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new(JwtRegisteredClaimNames.UniqueName, user.Username),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            // Nạp danh sách Role của User vào Token
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role,role));
            }
            var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"] ?? "1440");
            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims:claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credential
             );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public RefreshToken GenerateRefreshToken(Guid userId, string? ipAddress = null)
        {
            var randomBytes = new byte[64];
            // sử dụng using để tự động giải phóng cái rng để tránh memory leak
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            var daysToExpire = int.Parse(_configuration["JwtSettings:RefreshTokenExpiryDays"] ?? "7");
            return new RefreshToken
            {
                UserId = userId,
                Token = Convert.ToBase64String(randomBytes),
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(daysToExpire),
                CreatedByIp = ipAddress
            };
        }
    }
}
