using DATN_BE.Modules.Workspaces.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DATN_BE.Modules.Workspaces.infrastructure.Configurations
{
    public class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
    {
        public void Configure(EntityTypeBuilder<Workspace> builder)
        {
            builder.ToTable("Workspaces");
            builder.HasKey(w => w.Id);
            builder.Property(w => w.Name).IsRequired().HasMaxLength(100);
            builder.Property(w => w.Slug).IsRequired().HasMaxLength(120);
            builder.Property(w => w.LogoUrl).HasMaxLength(500);
            // Cấu hình quan hệ Owner (User 1 - N Workspace)
            builder.HasOne(w => w.Owner).WithMany(u => u.OwnedWorkspaces).HasForeignKey(w => w.OwnerId).OnDelete(DeleteBehavior.Restrict);
            // Khi IsDeleted = false thì cái Slug sẽ là unique
            builder.HasIndex(w => w.Slug).IsUnique().HasFilter("\"IsDeleted\" = false");
        }
    }
}
