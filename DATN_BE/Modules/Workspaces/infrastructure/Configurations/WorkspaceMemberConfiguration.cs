using DATN_BE.Modules.Workspaces.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DATN_BE.Modules.Workspaces.infrastructure.Configurations
{
    public class WorkspaceMemberConfiguration : IEntityTypeConfiguration<WorkspaceMember>
    {
        public void Configure(EntityTypeBuilder<WorkspaceMember> builder)
        {
            builder.ToTable("WorkspaceMembers");
            // Cấu hình Khóa chính Hợp phần (Composite Primary Key: WorkspaceId + UserId)
            builder.HasKey(wm => new { wm.WorkspaceId, wm.UserId });

            builder.HasOne(wm => wm.Workspace)
                .WithMany(w => w.Members)
                .HasForeignKey(wm => wm.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(wm => wm.User)
                .WithMany(u => u.WorkspaceMembers)
                .HasForeignKey(wm => wm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(wm => wm.Role)
                .WithMany()
                .HasForeignKey(wm => wm.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
