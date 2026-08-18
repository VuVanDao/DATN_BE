using DATN_BE.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DATN_BE.Modules.Identity.infrastructure.Configurations
{
    public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> builder)
        {
            builder.ToTable("Permissions");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Code)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(p => p.Module)
                .IsRequired()
                .HasMaxLength(50);

            // Ràng buộc Unique mã quyền (Code)
            builder.HasIndex(p => p.Code).IsUnique();
        }
    }
}
