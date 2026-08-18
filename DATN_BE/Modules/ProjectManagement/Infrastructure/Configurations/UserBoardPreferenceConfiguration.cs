using DATN_BE.Modules.ProjectManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DATN_BE.Modules.ProjectManagement.Infrastructure.Configurations
{
    public class UserBoardPreferenceConfiguration : IEntityTypeConfiguration<UserBoardPreference>
    {
        public void Configure(EntityTypeBuilder<UserBoardPreference> builder)
        {
            builder.ToTable("UserBoardPreferences");

            // Composite Primary Key (UserId + BoardId)
            builder.HasKey(ubp => new { ubp.UserId, ubp.BoardId });

            builder.Property(ubp => ubp.IsPinned)
                .HasDefaultValue(false);

            builder.HasOne(ubp => ubp.User)
                .WithMany(u => u.UserBoardPreferences)
                .HasForeignKey(ubp => ubp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ubp => ubp.Board)
                .WithMany(b => b.UserBoardPreferences)
                .HasForeignKey(ubp => ubp.BoardId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
