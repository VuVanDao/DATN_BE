
using DATN_BE.Modules.Identity.Application.Interfaces;
using DATN_BE.Modules.Identity.infrastructure.Services;
using DATN_BE.Modules.Identity.Presentation.BackgroundJob;
using DATN_BE.Modules.Notifications.Infrastructure.Services;
using DATN_BE.Modules.Notifications.Presentation.Hubs;
using DATN_BE.Modules.ProjectManagement.Presentation.Hubs;
using DATN_BE.Shared.Application.Behaviors;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Infrastructure.Persistence;
using DATN_BE.Shared.Infrastructure.Services;
using DATN_BE.Shared.Presentation.Middlewares;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
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
            builder.Services.AddSignalR();
            var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
                                 ?? new[] { "http://localhost:4200" };
            builder.Services.AddCors(options => options.AddPolicy("AllowAngularApp", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            }));
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            // 2. DbContext & Interface
            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddScoped<IApplicationDbContext>(provider 
                => provider.GetRequiredService<ApplicationDbContext>());
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
            builder.Services.AddScoped<IProjectAuthorizationService, ProjectAuthorizationService>();
            builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
            builder.Services.AddScoped<IEmailService, GmailSmtpEmailService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddHostedService<SendPasswordResetTokenByEmailWorker>();

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

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];

                            if (!string.IsNullOrEmpty(accessToken) &&
                                (context.HttpContext.Request.Path.StartsWithSegments(BoardHub.Route) ||
                                 context.HttpContext.Request.Path.StartsWithSegments(NotificationHub.Route)))
                            {
                                context.Token = accessToken;
                            }

                            return Task.CompletedTask;
                        }
                    };
                });
            builder.Services.AddAuthorization();

            var app = builder.Build();

            // 7. Chạy Migration & Data Seeder khi ứng dụng khởi động
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await context.Database.MigrateAsync();
                await DataSeeder.SeedAsync(context);
            }

            // 8. Middleware Pipeline
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
            }

            app.UseHttpsRedirection();

            app.UseCors("AllowAngularApp");

            app.UseAuthentication();

            app.UseAuthorization();

            app.MapHub<BoardHub>(BoardHub.Route);
            app.MapHub<NotificationHub>(NotificationHub.Route);
            app.MapControllers();

            await app.RunAsync();
        }
    }
}
