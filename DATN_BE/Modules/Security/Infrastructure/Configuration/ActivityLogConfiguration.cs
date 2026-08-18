using DATN_BE.Modules.Security.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DATN_BE.Modules.Security.Infrastructure.Configuration
{
    public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
    {
        public void Configure(EntityTypeBuilder<ActivityLog> builder)
        {
            builder.ToTable("ActivityLogs");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.EntityType)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(a => a.ActionType)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);

            // Cấu hình kiểu dữ liệu JSONB trong PostgreSQL
            builder.Property(a => a.DetailsJson)
                .HasColumnType("jsonb");

            // Quan hệ với User thực hiện hành động
            builder.HasOne(a => a.User)
                .WithMany(u => u.UserActivityLogs)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Chỉ mục giúp truy vấn lịch sử nhanh theo đối tượng và theo user
            builder.HasIndex(a => new { a.EntityType, a.EntityId });
            builder.HasIndex(a => new { a.UserId, a.CreatedAt });
        }
    }
}
