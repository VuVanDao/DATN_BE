using DATN_BE.Modules.ProjectManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DATN_BE.Modules.ProjectManagement.Infrastructure.Configurations
{
    public class BoardConfiguration : IEntityTypeConfiguration<Board>
    {
        public void Configure(EntityTypeBuilder<Board> builder)
        {
            builder.ToTable("Boards");
            builder.HasKey(b => b.Id);
            builder.Property(b => b.Title).IsRequired().HasMaxLength(100);
            builder.Property(b => b.Slug).IsRequired().HasMaxLength(120);
            builder.Property(b => b.Type).IsRequired().HasMaxLength(20);
            builder.HasOne(b => b.Project).WithMany(p => p.Boards)
                .HasForeignKey(p => p.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(b => new { b.ProjectId, b.Slug })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false");
        }
    }
}
