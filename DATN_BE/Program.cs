
using DATN_BE.Shared.Application.Behaviors;
using DATN_BE.Shared.Application.Interfaces;
using DATN_BE.Shared.Infrastructure.Persistence;
using DATN_BE.Shared.Presentation.Middlewares;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DATN_BE
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();
            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddScoped<IApplicationDbContext>(provider 
                => provider.GetRequiredService<ApplicationDbContext>());
            // Đăng ký MediatR cho toàn bộ Assembly
            builder.Services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
                // Đăng ký Validation Pipeline Behavior
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            });
            // Đăng ký tự động toàn bộ FluentValidation Validators
            builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

            var app = builder.Build();
            // Sử dụng Global Exception Middleware ở đầu pipeline
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
