using DATN_BE.Modules.Security.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DATN_BE.Modules.Security.Infrastructure.Configuration
{
    public class LoginHistoryConfiguration : IEntityTypeConfiguration<LoginHistory>
    {
        public void Configure(EntityTypeBuilder<LoginHistory> builder)
        {
            builder.ToTable("LoginHistories");
            builder.HasKey(lh => lh.Id);
            builder.Property(lh => lh.EmailAttempted)
                .IsRequired()
                .HasMaxLength(256);
            builder.Property(lh => lh.IpAddress)
                .HasMaxLength(100);
            builder.Property(lh => lh.UserAgent)
                .HasMaxLength(500);
            builder.Property(lh => lh.FailureReason)
                .HasMaxLength(256);
            builder.HasIndex(lh => lh.UserId);
            builder.HasIndex(lh => lh.CreatedAt);
            builder.HasOne(lh => lh.User)
                .WithMany()
                .HasForeignKey(lh => lh.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
