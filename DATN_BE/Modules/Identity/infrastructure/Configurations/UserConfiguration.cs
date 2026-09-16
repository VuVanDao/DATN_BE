using DATN_BE.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DATN_BE.Modules.Identity.infrastructure.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Email).IsRequired().HasMaxLength(255);
            builder.Property(u => u.Username).IsRequired().HasMaxLength(100);
            builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);
            builder.Property(u => u.AvatarUrl).HasMaxLength(500);

            // Partial Unique Indexes
            builder.HasIndex(u => u.Email).IsUnique().HasFilter("\"IsDeleted\" = false");
            builder.HasIndex(u => u.Username).IsUnique().HasFilter("\"IsDeleted\" = false");

            // Role Relationship (1 Role - Many Users)
            builder.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
