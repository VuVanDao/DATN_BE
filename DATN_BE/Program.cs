
using DATN_BE.Modules.Identity.Application.Interfaces;
using DATN_BE.Modules.Identity.infrastructure.Services;
using DATN_BE.Shared.Application.Behaviors;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Infrastructure.Persistence;
using DATN_BE.Shared.Infrastructure.Services;
using DATN_BE.Shared.Presentation.Middlewares;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace DATN_BE
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. Add Controllers
            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            // 2. DbContext & Interface
            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddScoped<IApplicationDbContext>(provider 
                => provider.GetRequiredService<ApplicationDbContext>());
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

            // 3. Register Identity Services
            builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

            // 4. MediatR & Pipeline Behaviors
            builder.Services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
                // Đăng ký Validation Pipeline Behavior
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            });

            // 5. FluentValidation, đăng ký tự động toàn bộ FluentValidation Validators
            builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

            // 6. JWT Authentication Configuration
            var jwtSettings = builder.Configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"]!;
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings["Issuer"],
                        ValidAudience = jwtSettings["Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                        ClockSkew = TimeSpan.Zero
                    };
                });
            builder.Services.AddAuthorization();

            var app = builder.Build();

            // 7. Chạy Data Seeder khi ứng dụng khởi động
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await DataSeeder.SeedAsync(context);
            }

            // 8. Middleware Pipeline
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();

            app.UseAuthorization();

            app.MapControllers();

            await app.RunAsync();
        }
    }
}
