using DATN_BE.Modules.ProjectManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DATN_BE.Modules.ProjectManagement.Infrastructure.Configurations
{
    public class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
    {
        public void Configure(EntityTypeBuilder<ProjectMember> builder)
        {
            builder.ToTable("ProjectMembers");
            builder.HasKey(pm => new { pm.ProjectId, pm.UserId });

            builder.HasOne(pm => pm.Project)
                .WithMany(p => p.ProjectMembers)
                .HasForeignKey(pm => pm.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pm => pm.User)
                .WithMany(u => u.ProjectMembers)
                .HasForeignKey(pm => pm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pm => pm.Role)
                .WithMany()
                .HasForeignKey(pm => pm.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(pm => pm.JobTitle)
                .HasMaxLength(100)
                .IsRequired(false);
        }
    }
}
