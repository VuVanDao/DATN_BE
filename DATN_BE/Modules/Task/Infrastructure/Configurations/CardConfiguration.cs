using DATN_BE.Modules.Task.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DATN_BE.Modules.Task.Infrastructure.Configurations
{
    public class CardConfiguration : IEntityTypeConfiguration<Card>
    {
        public void Configure(EntityTypeBuilder<Card> builder)
        {
            builder.ToTable("Cards");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Title).IsRequired().HasMaxLength(200);
            builder.Property(c => c.Type)
                .HasConversion<string>()
                .HasMaxLength(20);
            builder.Property(c => c.Priority)
                .HasConversion<string>()
                .HasMaxLength(20);
            builder.Property(c => c.CoverUrl).HasMaxLength(500);

            // Quan hệ Column (1 Column có nhiều Card)
            builder.HasOne(c => c.Column)
                .WithMany(col => col.Cards)
                .HasForeignKey(c => c.ColumnId)
                .OnDelete(DeleteBehavior.Cascade);

            // Quan hệ Assignee (1 User được giao nhiều Card)
            builder.HasOne(c => c.Assignee)
                .WithMany(u => u.AssignedCard)
                .HasForeignKey(c => c.AssigneeId)
                .OnDelete(DeleteBehavior.SetNull);

            // Quan hệ Reporter (1 User tạo nhiều Card)
            builder.HasOne(c => c.Reporter)
                .WithMany(u => u.ReportedCard)
                .HasForeignKey(c => c.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);

            // Partial Unique Index: Position không trùng nhau trong CÙNG 1 Column
            builder.HasIndex(c => new { c.ColumnId, c.Position })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false");
        }
    }
}
