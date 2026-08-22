using DATN_BE.Modules.ProjectManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DATN_BE.Modules.ProjectManagement.Infrastructure.Configurations
{
    public class ColumnConfiguration : IEntityTypeConfiguration<Column>
    {
        public void Configure(EntityTypeBuilder<Column> builder)
        {
            builder.ToTable("Columns");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Title).IsRequired().HasMaxLength(100);
            builder.HasOne(c => c.Board).WithMany(b => b.Columns)
                .HasForeignKey(c => c.BoardId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index thông thường phục vụ ORDER BY Position nhanh chóng
            builder.HasIndex(c => new { c.BoardId, c.Position });
        }
    }
}
