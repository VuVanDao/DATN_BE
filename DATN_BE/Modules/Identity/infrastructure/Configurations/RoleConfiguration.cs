using DATN_BE.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DATN_BE.Modules.Identity.infrastructure.Configurations
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("Roles");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Name)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(r => r.Scope)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(r => r.Description)
                .HasMaxLength(255);

            builder.Property(r => r.IsSystemRole)
                .HasDefaultValue(false);

            // Ràng buộc Unique tên Role
            builder.HasIndex(r => r.Name).IsUnique();
        }
    }
}
